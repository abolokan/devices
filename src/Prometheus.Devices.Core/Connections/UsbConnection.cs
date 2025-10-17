using LibUsbDotNet;
using LibUsbDotNet.LibUsb;
using LibUsbDotNet.Main;
using Prometheus.Devices.Core.Interfaces;

namespace Prometheus.Devices.Core.Connections
{
    /// <summary>
    /// USB connection implementation using LibUsbDotNet
    /// </summary>
    public class UsbConnection : BaseConnection
    {
        private readonly int _vendorId;
        private readonly int _productId;
        private readonly string? _serialNumber;
        private readonly byte _readEndpointId;
        private readonly byte _writeEndpointId;

        private UsbContext? _context;
        private IUsbDevice? _usbDevice;
        private UsbEndpointReader? _reader;
        private UsbEndpointWriter? _writer;

        public override string ConnectionInfo => $"USB: VID={_vendorId:X4}, PID={_productId:X4}" + 
                                                  (_serialNumber != null ? $", SN={_serialNumber}" : "");

        public int VendorId => _vendorId;
        public int ProductId => _productId;
        public string? SerialNumber => _serialNumber;

        /// <summary>
        /// Create USB connection
        /// </summary>
        /// <param name="vendorId">USB Vendor ID (e.g., 0x1504 for Bixolon)</param>
        /// <param name="productId">USB Product ID</param>
        /// <param name="serialNumber">Optional serial number for specific device</param>
        /// <param name="writeEndpointId">Write endpoint ID (default: 0x01)</param>
        /// <param name="readEndpointId">Read endpoint ID (default: 0x81)</param>
        public UsbConnection(
            int vendorId, 
            int productId, 
            string? serialNumber = null,
            byte writeEndpointId = 0x01,
            byte readEndpointId = 0x81)
        {
            _vendorId = vendorId;
            _productId = productId;
            _serialNumber = serialNumber;
            _writeEndpointId = writeEndpointId;
            _readEndpointId = readEndpointId;
        }

        public override async Task OpenAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (Status == ConnectionStatus.Connected)
                return;

            await Task.Run(() =>
            {
                try
                {
                    SetStatus(ConnectionStatus.Connecting, $"Searching for USB device VID={_vendorId:X4}, PID={_productId:X4}...");

                    // Find and open USB device using LibUsbDotNet 3.x API
                    _context = new UsbContext();
                    var deviceList = _context.List();
                    
                    var targetDevice = deviceList.FirstOrDefault(d => 
                        d.VendorId == _vendorId && 
                        d.ProductId == _productId);

                    if (targetDevice == null)
                        throw new ConnectionException($"USB device not found (VID={_vendorId:X4}, PID={_productId:X4})");

                    // Open the device
                    _usbDevice = targetDevice;
                    _usbDevice.Open();

                    // Claim interface 0
                    _usbDevice.ClaimInterface(0);

                    // Open endpoints (LibUsbDotNet 3.x API)
                    _reader = _usbDevice.OpenEndpointReader((ReadEndpointID)_readEndpointId);
                    _writer = _usbDevice.OpenEndpointWriter((WriteEndpointID)_writeEndpointId);

                    SetStatus(ConnectionStatus.Connected, $"USB device connected (VID={_vendorId:X4}, PID={_productId:X4})");
                }
                catch (Exception ex) when (ex is not ConnectionException)
                {
                    CleanupResources();
                    SetStatus(ConnectionStatus.Error, "USB connection error", ex);
                    throw new ConnectionException($"Failed to connect to USB device VID={_vendorId:X4}, PID={_productId:X4}", ex);
                }
            }, cancellationToken);
        }

        public override async Task CloseAsync(CancellationToken cancellationToken = default)
        {
            if (Status == ConnectionStatus.Disconnected)
                return;

            await Task.Run(() =>
            {
                try
                {
                    SetStatus(ConnectionStatus.Disconnecting, "Disconnecting USB device...");
                    CleanupResources();
                    SetStatus(ConnectionStatus.Disconnected, "USB device disconnected");
                }
                catch (Exception ex)
                {
                    SetStatus(ConnectionStatus.Error, "USB disconnection error", ex);
                    throw;
                }
            }, cancellationToken);
        }

        public override async Task<int> SendAsync(byte[] data, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (Status != ConnectionStatus.Connected)
                throw new InvalidOperationException("USB connection not established");

            if (data == null || data.Length == 0)
                throw new ArgumentException("Data cannot be empty", nameof(data));

            if (_writer == null)
                throw new InvalidOperationException("USB writer not initialized");

            return await Task.Run(() =>
            {
                try
                {
                    int bytesWritten;
                    var error = _writer.Write(data, 5000, out bytesWritten);
                    
                    if (error != Error.Success)
                        throw new ConnectionException($"USB write failed with error: {error}");
                        
                    return bytesWritten;
                }
                catch (Exception ex) when (ex is not ConnectionException)
                {
                    SetStatus(ConnectionStatus.Error, "USB send data error", ex);
                    throw new ConnectionException("Error sending data via USB", ex);
                }
            }, cancellationToken);
        }

        public override async Task<byte[]> ReceiveAsync(int bufferSize = 4096, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (Status != ConnectionStatus.Connected)
                throw new InvalidOperationException("USB connection not established");

            if (_reader == null)
                throw new InvalidOperationException("USB reader not initialized");

            return await Task.Run(() =>
            {
                try
                {
                    byte[] buffer = new byte[bufferSize];
                    int bytesRead;
                    var error = _reader.Read(buffer, 5000, out bytesRead);

                    if (error != Error.Success && error != Error.Timeout)
                        throw new ConnectionException($"USB read failed with error: {error}");

                    if (bytesRead == 0)
                        return Array.Empty<byte>();

                    byte[] result = new byte[bytesRead];
                    Array.Copy(buffer, result, bytesRead);
                    return result;
                }
                catch (Exception ex) when (ex is not ConnectionException)
                {
                    SetStatus(ConnectionStatus.Error, "USB receive data error", ex);
                    throw new ConnectionException("Error receiving data via USB", ex);
                }
            }, cancellationToken);
        }

        public override async Task<bool> PingAsync(CancellationToken cancellationToken = default)
        {
            if (_usbDevice == null)
                return false;

            try
            {
                return _usbDevice.IsOpen;
            }
            catch
            {
                return false;
            }
        }

        private void CleanupResources()
        {
            try
            {
                // Close endpoints (LibUsbDotNet 3.x doesn't need Dispose for readers/writers)
                _reader = null;
                _writer = null;

                // Release interface and close device
                if (_usbDevice != null)
                {
                    try { _usbDevice.ReleaseInterface(0); } catch { }
                    try { _usbDevice.Close(); } catch { }
                    try { _usbDevice.Dispose(); } catch { }
                    _usbDevice = null;
                }

                if (_context != null)
                {
                    try
                    { _context.Dispose(); }
                    catch { }
                    _context = null;
                }
            }
            catch
            {
                // Suppress cleanup errors
            }
        }

        public override void Dispose()
        {
            if (_disposed)
                return;

            CleanupResources();
            base.Dispose();
        }

        /// <summary>
        /// Get list of all USB devices (for debugging/discovery)
        /// Cross-platform: Works on Windows and Linux
        /// </summary>
        public static UsbDeviceInfo[] GetAllUsbDevices()
        {
            var devices = new List<UsbDeviceInfo>();

            try
            {
                using var context = new UsbContext();
                var deviceList = context.List();

                foreach (var device in deviceList)
                {
                    devices.Add(new UsbDeviceInfo
                    {
                        VendorId = device.VendorId,
                        ProductId = device.ProductId,
                        SerialNumber = "N/A",
                        Description = $"USB Device VID={device.VendorId:X4} PID={device.ProductId:X4}",
                        Manufacturer = "Unknown",
                        Product = "Unknown"
                    });
                }
            }
            catch
            {
                // Return empty list if USB context fails
            }

            return devices.ToArray();
        }

        /// <summary>
        /// Find USB device by VID/PID
        /// </summary>
        public static UsbDeviceInfo? FindDevice(int vendorId, int productId)
        {
            var allDevices = GetAllUsbDevices();
            return allDevices.FirstOrDefault(d => d.VendorId == vendorId && d.ProductId == productId);
        }
    }

    /// <summary>
    /// USB device information
    /// </summary>
    public class UsbDeviceInfo
    {
        public int VendorId { get; set; }
        public int ProductId { get; set; }
        public string? SerialNumber { get; set; }
        public string? Description { get; set; }
        public string? Manufacturer { get; set; }
        public string? Product { get; set; }

        public override string ToString() => 
            $"VID={VendorId:X4}, PID={ProductId:X4}, {Manufacturer} {Product}";
    }
}
