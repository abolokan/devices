using Prometheus.Devices.Core.Interfaces;
using Prometheus.Devices.Core.Drivers;
using Prometheus.Devices.Core.Profiles;
using Prometheus.Devices.Printers.Drivers.EscPos;
using DeviceWrappers.Devices.Printer;

namespace Prometheus.Devices.Common.Factories
{
    /// <summary>
    /// Factory for creating printer devices
    /// Supports: Driver-based (ESC/POS), Network, Serial, USB, Office printers
    /// Cross-platform: Windows, Linux, macOS
    /// </summary>
    public static class PrinterFactory
    {
        // ============= DRIVER-BASED PRINTERS (Recommended for Linux) =============

        /// <summary>
        /// Create driver-based printer with explicit driver and profile
        /// Best for: Receipt printers, label printers, packaging printers (ESC/POS, ZPL)
        /// Cross-platform: ✅ Windows, Linux, macOS
        /// </summary>
        /// <param name="connection">Connection (TCP, Serial, USB)</param>
        /// <param name="profile">Printer profile (optional)</param>
        /// <param name="driver">Printer driver (ESC/POS, ZPL, etc.)</param>
        /// <param name="deviceId">Device ID (optional, auto-generated)</param>
        /// <param name="deviceName">Device name (optional, from profile)</param>
        public static IPrinter CreateDriver(
            IConnection connection,
            PrinterProfile? profile = null,
            IPrinterDriver? driver = null,
            string? deviceId = null,
            string? deviceName = null)
        {
            driver ??= ResolveDriver(profile);
            deviceId ??= $"DRV_PRN_{profile?.Manufacturer}_{profile?.Model}";
            deviceName ??= $"{profile?.Manufacturer} {profile?.Model}";
            return new DriverPrinter(deviceId, deviceName, connection, driver, profile);
        }

        /// <summary>
        /// Create ESC/POS printer (most common receipt/POS printers: Bixolon, Epson TM, Star)
        /// Cross-platform: ✅ Windows, Linux, macOS
        /// </summary>
        public static IPrinter CreateEscPos(
            IConnection connection,
            PrinterProfile? profile = null,
            string? deviceId = null,
            string? deviceName = null)
        {
            return CreateDriver(connection, profile, new EscPosDriver(), deviceId, deviceName);
        }       

        /// <summary>
        /// Resolve printer driver from profile protocol
        /// </summary>
        public static IPrinterDriver ResolveDriver(PrinterProfile? profile)
        {
            var proto = (profile?.Protocol ?? "").ToUpperInvariant();
            return proto switch
            {
                "ESC_POS" or "ESCPOS" => new EscPosDriver(),
                "BIXOLON" => new EscPosDriver(), // Bixolon uses ESC/POS
                _ => new EscPosDriver() // Default to ESC/POS
            };
        }

        // ============= NETWORK PRINTERS =============

        /// <summary>
        /// Create network printer (TCP/IP)
        /// Automatically creates TCP connection
        /// </summary>
        /// <param name="ipAddress">Printer IP address</param>
        /// <param name="port">Printer port (default: 9100 for raw printing)</param>
        /// <param name="name">Device name (optional)</param>
        public static IPrinter CreateNetwork(string ipAddress, int port = 9100, string? name = null)
        {
            return NetworkPrinter.Create(ipAddress, port, name);
        }

        // ============= SERIAL PRINTERS =============

        /// <summary>
        /// Create serial printer (RS-232, COM port)
        /// Automatically creates Serial connection
        /// </summary>
        /// <param name="portName">Port name (COM1 on Windows, /dev/ttyUSB0 on Linux)</param>
        /// <param name="baudRate">Baud rate (default: 9600)</param>
        /// <param name="name">Device name (optional)</param>
        public static IPrinter CreateSerial(string portName, int baudRate = 9600, string? name = null)
        {
            return SerialPrinter.Create(portName, baudRate, name);
        }

        // ============= USB PRINTERS =============

        /// <summary>
        /// Create USB printer (direct USB communication)
        /// Automatically creates USB connection
        /// </summary>
        /// <param name="vendorId">USB Vendor ID</param>
        /// <param name="productId">USB Product ID</param>
        /// <param name="name">Device name (optional)</param>
        public static IPrinter CreateUsb(int vendorId, int productId, string? name = null)
        {
            return UsbPrinter.Create(vendorId, productId, name);
        }

        // ============= OFFICE PRINTERS (OS-managed) =============

        /// <summary>
        /// Create office printer (OS print spooler: Windows/Linux/macOS)
        /// Uses: Windows (PrintDocument), Linux (CUPS lpr), macOS (CUPS lpr)
        /// Warning: Linux CUPS may not fully support PJL commands
        /// </summary>
        /// <param name="systemPrinterName">System printer name</param>
        /// <param name="deviceName">Device name (optional)</param>
        public static IPrinter CreateOffice(string systemPrinterName, string? deviceName = null)
        {
            var platformPrinter = PlatformFactory.GetPrinter();
            var deviceId = $"OFFICE_PRINTER_{systemPrinterName}";
            var name = deviceName ?? systemPrinterName;
            return new OfficePrinter(deviceId, name, systemPrinterName, platformPrinter);
        }

        /// <summary>
        /// Get list of available office printers from OS
        /// Windows: via PrintDocument, Linux/macOS: via 'lpstat -p'
        /// </summary>
        public static async Task<string[]> GetAvailableOfficePrintersAsync()
        {
            var platformPrinter = PlatformFactory.GetPrinter();
            return await platformPrinter.GetAvailablePrintersAsync();
        }

        // ============= HELPERS =============

        /// <summary>
        /// Check if office printers are available
        /// </summary>
        public static async Task<bool> HasOfficePrintersAsync()
        {
            var printers = await GetAvailableOfficePrintersAsync();
            return printers.Length > 0;
        }
    }
}

