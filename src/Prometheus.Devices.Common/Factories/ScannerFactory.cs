using Prometheus.Devices.Abstractions.Interfaces;
using Prometheus.Devices.Scanners;
using System.IO.Ports;
using System.Runtime.InteropServices;

namespace Prometheus.Devices.Common.Factories
{
    /// <summary>
    /// Factory for creating scanner devices
    /// Supports: Office scanners (TWAIN on Windows, SANE on Linux)
    /// Cross-platform: Windows, Linux, macOS
    /// </summary>
    public static class ScannerFactory
    {
        // ============= OFFICE SCANNERS =============

        /// <summary>
        /// Create office scanner (OS-managed via TWAIN/SANE)
        /// Windows: TWAIN, Linux: SANE, macOS: TWAIN/SANE
        /// </summary>
        /// <param name="systemScannerName">System scanner name (TWAIN device name or SANE device string)</param>
        /// <param name="deviceName">Device name (optional)</param>
        public static IScanner CreateOffice(string systemScannerName, string? deviceName = null)
        {
            var platformScanner = PlatformFactory.GetScanner();
            var deviceId = $"OFFICE_SCANNER_{systemScannerName}";
            var name = deviceName ?? systemScannerName;
            return new OfficeScanner(deviceId, name, systemScannerName, platformScanner);
        }

        /// <summary>
        /// Get list of available office scanners from OS
        /// Windows: via TWAIN, Linux: via 'scanimage -L'
        /// </summary>
        public static async Task<string[]> GetAvailableScannersAsync()
        {
            var platformScanner = PlatformFactory.GetScanner();
            return await platformScanner.GetAvailableScannersAsync();
        }

        // ============= HELPERS =============

        /// <summary>
        /// Check if any scanners are available
        /// </summary>
        public static async Task<bool> HasScannersAsync()
        {
            var scanners = await GetAvailableScannersAsync();
            return scanners.Length > 0;
        }

        /// <summary>
        /// Get scanner count
        /// </summary>
        public static async Task<int> GetCountAsync()
        {
            var scanners = await GetAvailableScannersAsync();
            return scanners.Length;
        }

        // ============= BARCODE SCANNERS =============

        /// <summary>
        /// Create Zebra SE4107 barcode scanner with USB connection
        /// </summary>
        /// <param name="vendorId">USB Vendor ID (default: 0x05E0 for Zebra)</param>
        /// <param name="productId">USB Product ID (default: 0x1900 for SE4107)</param>
        /// <param name="deviceName">Device name (optional)</param>
        public static IBarcodeScanner CreateZebraUsb(int vendorId = 0x05E0, int productId = 0x1900, string? deviceName = null)
        {
            return ZebraBarcodeScanner.CreateUsb(vendorId, productId, deviceName);
        }

        /// <summary>
        /// Create Zebra SE4107 barcode scanner with Serial connection
        /// </summary>
        /// <param name="portName">Serial port name (e.g., "COM3" on Windows, "/dev/ttyUSB0" on Linux)</param>
        /// <param name="baudRate">Baud rate (default: 9600)</param>
        /// <param name="deviceName">Device name (optional)</param>
        public static IBarcodeScanner CreateZebraSerial(string portName, int baudRate = 9600, string? deviceName = null)
        {
            return ZebraBarcodeScanner.CreateSerial(portName, baudRate, deviceName);
        }

        /// <summary>
        /// Create Zebra barcode scanner with auto-detection
        /// Tries USB first, then falls back to Serial on available ports
        /// </summary>
        /// <param name="connectionTimeout">Timeout for each connection attempt in milliseconds (default: 2000)</param>
        /// <param name="scannerName">Optional custom scanner name</param>
        /// <returns>Connected scanner or null if not found</returns>
        public static async Task<IBarcodeScanner?> CreateZebraAutoAsync(
            int connectionTimeout = 2000,
            string? scannerName = null)
        {
            // Try USB connection first
            var usbScanner = await TryConnectUsbAsync(connectionTimeout, scannerName);
            if (usbScanner != null)
                return usbScanner;

            // Try Serial connection on available ports
            var serialScanner = await TryConnectSerialAsync(connectionTimeout, scannerName);
            if (serialScanner != null)
                return serialScanner;

            return null; // No scanner found
        }

        /// <summary>
        /// Try to connect USB Zebra scanner
        /// </summary>
        private static async Task<IBarcodeScanner?> TryConnectUsbAsync(
            int timeout,
            string? scannerName)
        {
            IBarcodeScanner? scanner = null;
            try
            {
                scanner = CreateZebraUsb(deviceName: scannerName);
                
                using var cts = new CancellationTokenSource(timeout);
                await scanner.ConnectAsync(cts.Token);
                await scanner.InitializeAsync(cts.Token);
                
                return scanner;
            }
            catch
            {
                // Clean up on failure
                scanner?.Dispose();
                return null;
            }
        }

        /// <summary>
        /// Try to connect Serial Zebra scanner on available ports
        /// </summary>
        private static async Task<IBarcodeScanner?> TryConnectSerialAsync(
            int timeout,
            string? scannerName)
        {
            var ports = GetAvailableSerialPorts();
            
            foreach (var port in ports)
            {
                var scanner = await TryConnectSerialPortAsync(port, timeout, scannerName);
                if (scanner != null)
                    return scanner;
            }

            return null;
        }

        /// <summary>
        /// Try to connect to a specific serial port
        /// </summary>
        private static async Task<IBarcodeScanner?> TryConnectSerialPortAsync(
            string port,
            int timeout,
            string? scannerName)
        {
            IBarcodeScanner? scanner = null;
            try
            {
                scanner = CreateZebraSerial(port, deviceName: scannerName);
                
                using var cts = new CancellationTokenSource(timeout);
                await scanner.ConnectAsync(cts.Token);
                await scanner.InitializeAsync(cts.Token);
                
                return scanner;
            }
            catch
            {
                // Clean up on failure
                scanner?.Dispose();
                return null;
            }
        }

        /// <summary>
        /// Get list of available serial ports for the current OS
        /// </summary>
        private static string[] GetAvailableSerialPorts()
        {
            try
            {
                // Get system serial ports
                var systemPorts = SerialPort.GetPortNames();
                
                if (systemPorts.Length > 0)
                    return systemPorts;

                // Fallback to default ports by OS
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return new[] { "COM3", "COM4", "COM5", "COM6", "COM7" };
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    return new[] { "/dev/ttyUSB0", "/dev/ttyUSB1", "/dev/ttyACM0", "/dev/ttyS0" };
                }
                else // macOS
                {
                    return new[] { "/dev/tty.usbserial", "/dev/cu.usbserial" };
                }
            }
            catch
            {
                // If SerialPort.GetPortNames() fails, return default ports
                return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? new[] { "COM3", "COM4", "COM5" }
                    : new[] { "/dev/ttyUSB0", "/dev/ttyUSB1" };
            }
        }

        /// <summary>
        /// Search for all Zebra scanners (USB and Serial)
        /// </summary>
        /// <param name="connectionTimeout">Timeout for each connection attempt</param>
        /// <returns>List of found scanners</returns>
        public static async Task<List<IBarcodeScanner>> FindAllZebraScannersAsync(
            int connectionTimeout = 2000)
        {
            var scanners = new List<IBarcodeScanner>();

            // Try USB
            var usbScanner = await TryConnectUsbAsync(connectionTimeout, "Zebra USB Scanner");
            if (usbScanner != null)
                scanners.Add(usbScanner);

            // Try all Serial ports
            var ports = GetAvailableSerialPorts();
            foreach (var port in ports)
            {
                var scanner = await TryConnectSerialPortAsync(port, connectionTimeout, $"Zebra {port}");
                if (scanner != null)
                    scanners.Add(scanner);
            }

            return scanners;
        }
    }
}

