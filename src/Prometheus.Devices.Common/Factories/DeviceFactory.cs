using System.Runtime.InteropServices;
using OpenCvSharp;
using Prometheus.Devices.Core.Interfaces;
using Prometheus.Devices.Core.Platform;
using Prometheus.Devices.Core.Drivers;
using Prometheus.Devices.Core.Profiles;
using Prometheus.Devices.Common.Platform.Windows;
using Prometheus.Devices.Common.Platform.Linux;
using Prometheus.Devices.Printers.Drivers.EscPos;
using Prometheus.Devices.Printers.Drivers.Bixolon;
using DeviceWrappers.Devices.Camera;
using DeviceWrappers.Devices.Printer;
using DeviceWrappers.Devices.Scanner;


namespace Prometheus.Devices.Common.Factories
{
    /// <summary>
    /// Централизованная фабрика для создания устройств
    /// </summary>
    public static class DeviceFactory
    {
        // ============= CAMERAS =============

        public static ICamera CreateLocalCamera(int index = 0, string name = null)
        {
            return new LocalCamera(index, deviceName: name ?? $"Local Camera #{index}");
        }

        public static int[] EnumerateLocalCameraIndices(int maxProbe = 10)
        {
            var indices = new List<int>();
            for (int i = 0; i < maxProbe; i++)
            {
                using var cap = new VideoCapture(i);
                if (cap.IsOpened())
                    indices.Add(i);
            }
            return indices.ToArray();
        }

        public static ICamera CreateIpCamera(string ipAddress, int port, string name = null)
        {
            return IpCamera.Create(ipAddress, port, name);
        }

        public static ICamera CreateUsbCamera(int vendorId, int productId, string name = null)
        {
            return UsbCamera.Create(vendorId, productId, name);
        }

        // ============= PRINTERS (Driver-based) =============

        public static IPrinter CreateDriverPrinter(
            IConnection connection,
            PrinterProfile profile,
            IPrinterDriver driver,
            string deviceId = null,
            string deviceName = null)
        {
            deviceId ??= $"DRV_PRN_{profile?.Manufacturer}_{profile?.Model}";
            deviceName ??= $"{profile?.Manufacturer} {profile?.Model}";
            return new DriverPrinter(deviceId, deviceName, connection, driver, profile);
        }

        public static IPrinterDriver ResolvePrinterDriver(PrinterProfile profile)
        {
            var proto = (profile?.Protocol ?? "").ToUpperInvariant();
            return proto switch
            {
                "BIXOLON" => new BixolonBk331Driver(),
                "ESC_POS" => new EscPosDriver(),
                _ => new EscPosDriver()
            };
        }

        public static IPrinter CreateNetworkPrinter(string ipAddress, int port = 9100, string name = null)
        {
            return NetworkPrinter.Create(ipAddress, port, name);
        }

        public static IPrinter CreateSerialPrinter(string portName, int baudRate = 9600, string name = null)
        {
            return SerialPrinter.Create(portName, baudRate, name);
        }

        public static IPrinter CreateUsbPrinter(int vendorId, int productId, string name = null)
        {
            return UsbPrinter.Create(vendorId, productId, name);
        }

        // ============= OFFICE PRINTERS (Cross-platform) =============

        public static IPrinter CreateOfficePrinter(string systemPrinterName, string deviceName = null)
        {
            var platformPrinter = GetPlatformPrinter();
            var deviceId = $"OFFICE_PRINTER_{systemPrinterName}";
            var name = deviceName ?? systemPrinterName;
            return new OfficePrinter(deviceId, name, systemPrinterName, platformPrinter);
        }

        public static async Task<string[]> GetAvailableOfficePrintersAsync()
        {
            var platformPrinter = GetPlatformPrinter();
            return await platformPrinter.GetAvailablePrintersAsync();
        }

        // ============= SCANNERS (Cross-platform) =============

        public static IScanner CreateOfficeScanner(string systemScannerName, string deviceName = null)
        {
            var platformScanner = GetPlatformScanner();
            var deviceId = $"OFFICE_SCANNER_{systemScannerName}";
            var name = deviceName ?? systemScannerName;
            return new OfficeScanner(deviceId, name, systemScannerName, platformScanner);
        }

        public static async Task<string[]> GetAvailableScannersAsync()
        {
            var platformScanner = GetPlatformScanner();
            return await platformScanner.GetAvailableScannersAsync();
        }

        // ============= PLATFORM DETECTION =============

        public static IPlatformPrinter GetPlatformPrinter()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return new WindowsPlatformPrinter();
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return new LinuxPlatformPrinter();
            else
                throw new PlatformNotSupportedException("Platform not supported");
        }

        public static IPlatformScanner GetPlatformScanner()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return new WindowsPlatformScanner();
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return new LinuxPlatformScanner();
            else
                throw new PlatformNotSupportedException("Platform not supported");
        }
    }
}

