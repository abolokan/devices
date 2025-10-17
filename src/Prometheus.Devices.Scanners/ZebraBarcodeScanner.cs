using Prometheus.Devices.Core.Connections;
using Prometheus.Devices.Core.Devices;
using Prometheus.Devices.Core.Interfaces;
using System.Text;

namespace Prometheus.Devices.Scanners
{
    /// <summary>
    /// Zebra SE4107 Barcode/QR Scanner
    /// Supports USB HID and Serial connections
    /// </summary>
    public class ZebraBarcodeScanner : BaseDevice, IBarcodeScanner
    {
        private BarcodeScannerSettings _settings;
        private bool _isScanning = false;
        private CancellationTokenSource? _scanningCts;
        private readonly object _scanLock = new object();
        private readonly Queue<BarcodeData> _scanQueue = new Queue<BarcodeData>();

        public override DeviceType DeviceType => DeviceType.BarcodeScanner;

        public BarcodeScannerSettings Settings
        {
            get => _settings;
            set => _settings = value ?? throw new ArgumentNullException(nameof(value));
        }

        public event EventHandler<BarcodeScannedEventArgs>? BarcodeScanned;

        /// <summary>
        /// Create Zebra barcode scanner with USB connection
        /// </summary>
        public static ZebraBarcodeScanner CreateUsb(int vendorId = 0x05E0, int productId = 0x1900, string? deviceName = null)
        {
            var connection = new UsbConnection(vendorId, productId);
            return new ZebraBarcodeScanner(
                $"ZEBRA_USB_{vendorId:X4}_{productId:X4}",
                deviceName ?? "Zebra SE4107 USB",
                connection);
        }

        /// <summary>
        /// Create Zebra barcode scanner with Serial connection
        /// </summary>
        public static ZebraBarcodeScanner CreateSerial(string portName, int baudRate = 9600, string? deviceName = null)
        {
            var connection = new SerialConnection(portName, baudRate);
            return new ZebraBarcodeScanner(
                $"ZEBRA_SERIAL_{portName}",
                deviceName ?? $"Zebra SE4107 {portName}",
                connection);
        }

        public ZebraBarcodeScanner(string deviceId, string deviceName, IConnection connection)
            : base(deviceId, deviceName, connection)
        {
            _settings = new BarcodeScannerSettings();
        }

        protected override async Task OnInitializeAsync(CancellationToken cancellationToken)
        {
            // Configure scanner on initialization
            await ConfigureScannerAsync(cancellationToken);
        }

        protected override Task<DeviceInfo> OnGetDeviceInfoAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(new DeviceInfo
            {
                DeviceId = DeviceId,
                DeviceName = DeviceName,
                DeviceType = DeviceType.BarcodeScanner,
                Manufacturer = "Zebra Technologies",
                Model = "SE4107",
                FirmwareVersion = "1.0",
                SerialNumber = DeviceId
            });
        }

        protected override async Task OnResetAsync(CancellationToken cancellationToken)
        {
            await StopScanningAsync(cancellationToken);
            lock (_scanLock)
            {
                _scanQueue.Clear();
            }
            await ConfigureScannerAsync(cancellationToken);
        }

        public Task<bool> StartScanningAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfNotReady();
            
            if (_isScanning)
                return Task.FromResult(true);

            _scanningCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _isScanning = true;
            
            // Start background scanning task
            _ = Task.Run(async () => await ScanningLoopAsync(_scanningCts.Token), _scanningCts.Token);
            
            SetStatus(DeviceStatus.Busy, "Scanning...");
            return Task.FromResult(true);
        }

        public Task<bool> StopScanningAsync(CancellationToken cancellationToken = default)
        {
            if (!_isScanning)
                return Task.FromResult(true);

            _scanningCts?.Cancel();
            _isScanning = false;
            SetStatus(DeviceStatus.Ready, "Stopped scanning");
            
            return Task.FromResult(true);
        }

        public async Task<BarcodeData> ScanBarcodeAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfNotReady();
            SetStatus(DeviceStatus.Busy, "Waiting for barcode...");

            try
            {
                // Send trigger command
                await SendTriggerCommandAsync(cancellationToken);

                // Wait for barcode data with timeout
                var timeout = Settings.ScanTimeout > 0 ? Settings.ScanTimeout : 5000;
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(timeout);

                var barcode = await ReadBarcodeAsync(cts.Token);
                
                SetStatus(DeviceStatus.Ready, "Barcode scanned");
                
                if (Settings.BeepOnScan)
                {
                    await BeepAsync(100, cancellationToken);
                }

                return barcode;
            }
            catch (OperationCanceledException)
            {
                SetStatus(DeviceStatus.Ready, "Scan timeout");
                throw new TimeoutException("Barcode scan timeout");
            }
            catch (Exception ex)
            {
                SetStatus(DeviceStatus.Error, $"Scan error: {ex.Message}");
                throw;
            }
        }

        public Task<BarcodeType[]> GetSupportedBarcodesAsync(CancellationToken cancellationToken = default)
        {
            // Zebra SE4107 supports most common barcode types
            var supported = new[]
            {
                BarcodeType.Code39,
                BarcodeType.Code93,
                BarcodeType.Code128,
                BarcodeType.EAN8,
                BarcodeType.EAN13,
                BarcodeType.UPCA,
                BarcodeType.UPCE,
                BarcodeType.Interleaved2of5,
                BarcodeType.Codabar,
                BarcodeType.QRCode,
                BarcodeType.DataMatrix,
                BarcodeType.PDF417,
                BarcodeType.Aztec
            };
            return Task.FromResult(supported);
        }

        public async Task<bool> SetEnabledBarcodesAsync(BarcodeType[] types, CancellationToken cancellationToken = default)
        {
            ThrowIfNotReady();
            
            // Store enabled types in settings
            Settings.EnabledBarcodes = types;
            
            // Send configuration commands to scanner
            await ConfigureBarcodeTypesAsync(types, cancellationToken);
            
            return true;
        }

        public async Task<bool> BeepAsync(int durationMs = 100, CancellationToken cancellationToken = default)
        {
            ThrowIfNotReady();
            
            try
            {
                // Zebra beep command format (example)
                var beepCommand = Encoding.ASCII.GetBytes($"<BEEP>{durationMs}</BEEP>\r\n");
                await Connection.SendAsync(beepCommand, cancellationToken);
                return true;
            }
            catch
            {
                return false;
            }
        }

        #region Private Methods

        private async Task ConfigureScannerAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Send initialization commands
                // Example: Set scan mode
                if (Settings.TriggerMode == TriggerMode.Automatic)
                {
                    await SendCommandAsync("<TRIGGER>AUTO</TRIGGER>", cancellationToken);
                }
                else
                {
                    await SendCommandAsync("<TRIGGER>MANUAL</TRIGGER>", cancellationToken);
                }

                // Configure enabled barcodes
                if (Settings.EnabledBarcodes != null)
                {
                    await ConfigureBarcodeTypesAsync(Settings.EnabledBarcodes, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to configure scanner: {ex.Message}", ex);
            }
        }

        private async Task ConfigureBarcodeTypesAsync(BarcodeType[] types, CancellationToken cancellationToken)
        {
            // Zebra scanner configuration commands
            // This is a simplified example - actual implementation depends on SDK
            foreach (var type in types)
            {
                var command = type switch
                {
                    BarcodeType.QRCode => "<ENABLE>QRCODE</ENABLE>",
                    BarcodeType.Code128 => "<ENABLE>CODE128</ENABLE>",
                    BarcodeType.EAN13 => "<ENABLE>EAN13</ENABLE>",
                    BarcodeType.DataMatrix => "<ENABLE>DATAMATRIX</ENABLE>",
                    _ => null
                };

                if (command != null)
                {
                    await SendCommandAsync(command, cancellationToken);
                }
            }
        }

        private async Task SendTriggerCommandAsync(CancellationToken cancellationToken)
        {
            // Send trigger command to start scan
            await SendCommandAsync("<SCAN>", cancellationToken);
        }

        private async Task SendCommandAsync(string command, CancellationToken cancellationToken)
        {
            var data = Encoding.ASCII.GetBytes(command + "\r\n");
            await Connection.SendAsync(data, cancellationToken);
            await Task.Delay(50, cancellationToken); // Small delay for command processing
        }

        private async Task<BarcodeData> ReadBarcodeAsync(CancellationToken cancellationToken)
        {
            var dataBuilder = new StringBuilder();

            while (!cancellationToken.IsCancellationRequested)
            {
                var buffer = await Connection.ReceiveAsync(1024, cancellationToken);
                
                if (buffer != null && buffer.Length > 0)
                {
                    var chunk = Encoding.ASCII.GetString(buffer);
                    dataBuilder.Append(chunk);

                    // Check if we have complete barcode data (ends with newline or carriage return)
                    var data = dataBuilder.ToString();
                    if (data.Contains('\r') || data.Contains('\n'))
                    {
                        return ParseBarcodeData(data.Trim());
                    }
                }

                await Task.Delay(10, cancellationToken);
            }

            throw new OperationCanceledException("Barcode read cancelled");
        }

        private BarcodeData ParseBarcodeData(string rawData)
        {
            // Parse barcode data from scanner response
            // Format depends on scanner configuration
            // Example: "QR:HelloWorld" or just "HelloWorld"
            
            var barcodeType = BarcodeType.QRCode; // Default
            var decodedData = rawData;

            // Try to detect barcode type from prefix
            if (rawData.StartsWith("QR:"))
            {
                barcodeType = BarcodeType.QRCode;
                decodedData = rawData.Substring(3);
            }
            else if (rawData.StartsWith("CODE128:"))
            {
                barcodeType = BarcodeType.Code128;
                decodedData = rawData.Substring(8);
            }
            else if (rawData.StartsWith("EAN13:"))
            {
                barcodeType = BarcodeType.EAN13;
                decodedData = rawData.Substring(6);
            }

            return new BarcodeData
            {
                Data = decodedData,
                Type = barcodeType,
                RawData = Encoding.UTF8.GetBytes(rawData),
                Timestamp = DateTime.Now,
                Quality = 100,
                ScannerId = DeviceId
            };
        }

        private async Task ScanningLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && _isScanning)
            {
                try
                {
                    var barcode = await ReadBarcodeAsync(cancellationToken);
                    
                    lock (_scanLock)
                    {
                        _scanQueue.Enqueue(barcode);
                    }

                    // Raise event
                    OnBarcodeScanned(new BarcodeScannedEventArgs { Barcode = barcode });

                    if (Settings.BeepOnScan)
                    {
                        await BeepAsync(100, cancellationToken);
                    }

                    // Small delay between scans in continuous mode
                    if (Settings.Mode == ScanMode.Continuous)
                    {
                        await Task.Delay(100, cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception)
                {
                    // Continue scanning on errors
                    await Task.Delay(500, cancellationToken);
                }
            }

            _isScanning = false;
        }

        protected virtual void OnBarcodeScanned(BarcodeScannedEventArgs e)
        {
            BarcodeScanned?.Invoke(this, e);
        }

        #endregion

        public override void Dispose()
        {
            if (!_disposed)
            {
                try
                {
                    StopScanningAsync().Wait(1000);
                }
                catch { }

                _scanningCts?.Dispose();
                
                lock (_scanLock)
                {
                    _scanQueue.Clear();
                }
            }
            base.Dispose();
        }
    }
}

