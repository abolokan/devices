using Prometheus.Devices.Abstractions.Interfaces;
using Prometheus.Devices.Infrastructure.Extensions;
using Prometheus.Devices.Common.Factories;

namespace Prometheus.Devices.Test.App.Tests
{
    /// <summary>
    /// Scanner tests (TWAIN/SANE)
    /// </summary>
    public static class ScannerTests
    {
        /// <summary>
        /// Test office scanner (Windows TWAIN / Linux SANE)
        /// </summary>
        public static async Task TestOfficeScannerAsync(IDeviceManager deviceManager)
        {
            Console.WriteLine();
            Console.WriteLine("=== SCANNER TEST ===");

            Console.WriteLine("Searching for available scanners...");
            var scanners = await ScannerFactory.GetAvailableScannersAsync();

            if (scanners.Length == 0)
            {
                Console.WriteLine("No scanners found in system.");
                return;
            }

            Console.WriteLine($"Found scanners: {scanners.Length}");
            for (int i = 0; i < scanners.Length; i++)
            {
                Console.WriteLine($"  {i + 1}. {scanners[i]}");
            }

            Console.WriteLine();
            Console.Write($"Select scanner (1-{scanners.Length}): ");
            if (!int.TryParse(Console.ReadLine(), out int selection) || selection < 1 || selection > scanners.Length)
            {
                Console.WriteLine("Invalid selection.");
                return;
            }

            var selectedScanner = scanners[selection - 1];
            Console.WriteLine($"Selected scanner: {selectedScanner}");

            IScanner scanner = ScannerFactory.CreateOffice(selectedScanner);

            // Register device in DeviceManager
            deviceManager.RegisterDevice(scanner);

            try
            {
                Console.WriteLine("Connecting to scanner...");
                await scanner.ConnectAsync();
                await scanner.InitializeAsync();

                var info = await scanner.GetDeviceInfoAsync();
                Console.WriteLine($"✓ Connected to {info.DeviceName}");
                Console.WriteLine($"  Manufacturer: {info.Manufacturer}");
                Console.WriteLine($"  Model: {info.Model}");

                Console.WriteLine();
                Console.WriteLine("Scanner settings:");
                Console.WriteLine($"  Resolution: {scanner.Settings.Resolution} DPI");
                Console.WriteLine($"  Color mode: {scanner.Settings.ColorMode}");

                Console.WriteLine();
                Console.Write("Change resolution? Current: {0} DPI (press Enter to skip or enter new value): ", scanner.Settings.Resolution);
                var resInput = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(resInput) && int.TryParse(resInput, out int newResolution))
                {
                    scanner.Settings.Resolution = newResolution;
                    Console.WriteLine($"Resolution set to {newResolution} DPI");
                }

                Console.WriteLine();
                Console.WriteLine("Scanning... (Place document on scanner)");
                var scannedImage = await scanner.ScanAsync();

                var filename = $"scan_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
                var filepath = Path.Combine(AppContext.BaseDirectory, filename);
                await scanner.SaveImageAsync(scannedImage, filepath);

                Console.WriteLine($"✓ Document scanned and saved:");
                Console.WriteLine($"  File: {filename}");
                Console.WriteLine($"  Path: {filepath}");
                Console.WriteLine($"  Resolution: {scannedImage.Resolution}");
                Console.WriteLine($"  Size: {scannedImage.Data.Length / 1024} KB");
                Console.WriteLine($"  Time: {scannedImage.Timestamp:dd.MM.yyyy HH:mm:ss}");
                Console.WriteLine($"✓ Device registered in DeviceManager with ID: {scanner.DeviceId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Scanner error: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
            }
        }
    }
}

