using Prometheus.Devices.Core.Interfaces;
using Prometheus.Devices.Core.Extensions;
using Prometheus.Devices.Common.Factories;

namespace Prometheus.Devices.Test.App.Tests
{
    /// <summary>
    /// Barcode/QR scanner tests (Zebra SE4107)
    /// </summary>
    public static class BarcodeScannerTests
    {
        /// <summary>
        /// Test Zebra SE4107 barcode scanner via USB
        /// </summary>
        public static async Task TestZebraUsbScannerAsync(IDeviceManager deviceManager)
        {
            Console.WriteLine();
            Console.WriteLine("=== BARCODE SCANNER TEST (USB) ===");
            Console.WriteLine();

            Console.Write("Enter USB Vendor ID (press Enter for default 0x05E0): ");
            var vendorInput = Console.ReadLine();
            int vendorId = 0x05E0;
            if (!string.IsNullOrWhiteSpace(vendorInput) && vendorInput.StartsWith("0x"))
            {
                vendorId = Convert.ToInt32(vendorInput, 16);
            }
            else if (!string.IsNullOrWhiteSpace(vendorInput) && int.TryParse(vendorInput, out var vid))
            {
                vendorId = vid;
            }

            Console.Write("Enter USB Product ID (press Enter for default 0x1900): ");
            var productInput = Console.ReadLine();
            int productId = 0x1900;
            if (!string.IsNullOrWhiteSpace(productInput) && productInput.StartsWith("0x"))
            {
                productId = Convert.ToInt32(productInput, 16);
            }
            else if (!string.IsNullOrWhiteSpace(productInput) && int.TryParse(productInput, out var pid))
            {
                productId = pid;
            }

            IBarcodeScanner scanner = ScannerFactory.CreateZebraUsb(vendorId, productId);

            await TestBarcodeScannerAsync(deviceManager, scanner);
        }

        /// <summary>
        /// Test Zebra SE4107 barcode scanner via Serial
        /// </summary>
        public static async Task TestZebraSerialScannerAsync(IDeviceManager deviceManager)
        {
            Console.WriteLine();
            Console.WriteLine("=== BARCODE SCANNER TEST (SERIAL) ===");
            Console.WriteLine();

            Console.Write("Enter Serial Port (e.g., COM3, /dev/ttyUSB0): ");
            var port = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(port))
            {
                Console.WriteLine("Serial port is required.");
                return;
            }

            Console.Write("Enter Baud Rate (press Enter for default 9600): ");
            var baudInput = Console.ReadLine();
            int baudRate = 9600;
            if (!string.IsNullOrWhiteSpace(baudInput) && int.TryParse(baudInput, out var rate))
            {
                baudRate = rate;
            }

            IBarcodeScanner scanner = ScannerFactory.CreateZebraSerial(port, baudRate);

            await TestBarcodeScannerAsync(deviceManager, scanner);
        }

        /// <summary>
        /// Test Zebra barcode scanner with auto-detection
        /// </summary>
        public static async Task TestZebraAutoScannerAsync(IDeviceManager deviceManager)
        {
            Console.WriteLine();
            Console.WriteLine("=== BARCODE SCANNER TEST (AUTO-DETECT) ===");
            Console.WriteLine();

            Console.Write("Enter connection timeout in ms (press Enter for default 2000): ");
            var timeoutInput = Console.ReadLine();
            int timeout = 2000;
            if (!string.IsNullOrWhiteSpace(timeoutInput) && int.TryParse(timeoutInput, out var t))
            {
                timeout = t;
            }

            Console.WriteLine($"Searching for Zebra scanner (timeout: {timeout}ms per attempt)...");
            Console.WriteLine("Trying:");
            Console.WriteLine("  1. USB (VID: 0x05E0, PID: 0x1900)");
            Console.WriteLine("  2. Available Serial ports");
            Console.WriteLine();

            var scanner = await ScannerFactory.CreateZebraAutoAsync(
                connectionTimeout: timeout,
                scannerName: "Zebra SE4107 Auto"
            );

            if (scanner == null)
            {
                Console.WriteLine("✗ No Zebra scanner found.");
                Console.WriteLine();
                Console.WriteLine("Troubleshooting:");
                Console.WriteLine("  - Check USB connection (Vendor ID: 0x05E0, Product ID: 0x1900)");
                Console.WriteLine("  - Check Serial port availability");
                Console.WriteLine("  - Verify scanner is powered on");
                Console.WriteLine("  - Try increasing timeout");
                
                Console.WriteLine();
                Console.Write("Search for all scanners? (y/n): ");
                if (Console.ReadLine()?.ToLower() == "y")
                {
                    Console.WriteLine("Searching all ports (this may take longer)...");
                    var allScanners = await ScannerFactory.FindAllZebraScannersAsync(timeout);
                    
                    if (allScanners.Count > 0)
                    {
                        Console.WriteLine($"✓ Found {allScanners.Count} scanner(s):");
                        for (int i = 0; i < allScanners.Count; i++)
                        {
                            var s = allScanners[i];
                            Console.WriteLine($"  {i + 1}. {s.DeviceName} (ID: {s.DeviceId})");
                        }
                        
                        // Use first scanner
                        scanner = allScanners[0];
                        Console.WriteLine($"Using: {scanner.DeviceName}");
                        
                        // Clean up others
                        for (int i = 1; i < allScanners.Count; i++)
                        {
                            allScanners[i].Dispose();
                        }
                    }
                    else
                    {
                        Console.WriteLine("✗ Still no scanners found.");
                        return;
                    }
                }
                else
                {
                    return;
                }
            }

            Console.WriteLine($"✓ Found scanner: {scanner.DeviceName}");
            Console.WriteLine($"  Device ID: {scanner.DeviceId}");
            Console.WriteLine();
            
            await TestBarcodeScannerAsync(deviceManager, scanner, isAlreadyConnected: true);
        }

        /// <summary>
        /// Common barcode scanner test logic
        /// </summary>
        private static async Task TestBarcodeScannerAsync(
            IDeviceManager deviceManager, 
            IBarcodeScanner scanner,
            bool isAlreadyConnected = false)
        {
            // Register device in DeviceManager
            deviceManager.RegisterDevice(scanner);

            try
            {
                if (!isAlreadyConnected)
                {
                    Console.WriteLine("Connecting to scanner...");
                    await scanner.ConnectAsync();
                    await scanner.InitializeAsync();
                }

                var info = await scanner.GetDeviceInfoAsync();
                Console.WriteLine($"✓ Connected to {info.DeviceName}");
                Console.WriteLine($"  Manufacturer: {info.Manufacturer}");
                Console.WriteLine($"  Model: {info.Model}");
                Console.WriteLine($"  Device ID: {scanner.DeviceId}");
                Console.WriteLine($"✓ Device registered in DeviceManager");

                // Get supported barcode types
                var supportedTypes = await scanner.GetSupportedBarcodesAsync();
                Console.WriteLine();
                Console.WriteLine($"Supported barcode types ({supportedTypes.Length}):");
                foreach (var type in supportedTypes.Take(10))
                {
                    Console.WriteLine($"  - {type}");
                }
                if (supportedTypes.Length > 10)
                    Console.WriteLine($"  ... and {supportedTypes.Length - 10} more");

                // Configure scanner
                Console.WriteLine();
                Console.WriteLine("Scanner settings:");
                Console.WriteLine($"  Mode: {scanner.Settings.Mode}");
                Console.WriteLine($"  Beep on scan: {scanner.Settings.BeepOnScan}");
                Console.WriteLine($"  Scan timeout: {scanner.Settings.ScanTimeout}ms");
                Console.WriteLine($"  Trigger mode: {scanner.Settings.TriggerMode}");

                // Single scan test
                Console.WriteLine();
                Console.Write("Scan a single barcode/QR code? (y/n): ");
                if (Console.ReadLine()?.ToLower() == "y")
                {
                    Console.WriteLine("Please scan a barcode/QR code...");
                    Console.WriteLine("(Timeout: 10 seconds)");
                    
                    try
                    {
                        scanner.Settings.ScanTimeout = 10000;
                        var barcode = await scanner.ScanBarcodeAsync();
                        
                        Console.WriteLine();
                        Console.WriteLine("✓ Barcode scanned successfully!");
                        Console.WriteLine($"  Type: {barcode.Type}");
                        Console.WriteLine($"  Data: {barcode.Data}");
                        Console.WriteLine($"  Quality: {barcode.Quality}%");
                        Console.WriteLine($"  Timestamp: {barcode.Timestamp:dd.MM.yyyy HH:mm:ss}");
                        
                        if (barcode.RawData != null)
                            Console.WriteLine($"  Raw data size: {barcode.RawData.Length} bytes");
                    }
                    catch (TimeoutException)
                    {
                        Console.WriteLine("✗ Scan timeout - no barcode detected");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"✗ Scan error: {ex.Message}");
                    }
                }

                // Continuous scan test
                Console.WriteLine();
                Console.Write("Start continuous scanning for 10 seconds? (y/n): ");
                if (Console.ReadLine()?.ToLower() == "y")
                {
                    int scanCount = 0;
                    var scannedCodes = new List<string>();

                    scanner.BarcodeScanned += (s, e) =>
                    {
                        scanCount++;
                        scannedCodes.Add(e.Barcode.Data);
                        Console.WriteLine($"  [{scanCount}] {e.Barcode.Type}: {e.Barcode.Data}");
                    };

                    Console.WriteLine("Starting continuous scan...");
                    Console.WriteLine("Scan multiple barcodes/QR codes now!");
                    
                    await scanner.StartScanningAsync();
                    await Task.Delay(10000);
                    await scanner.StopScanningAsync();

                    Console.WriteLine();
                    Console.WriteLine($"✓ Continuous scan completed.");
                    Console.WriteLine($"  Total scans: {scanCount}");
                    Console.WriteLine($"  Unique codes: {scannedCodes.Distinct().Count()}");
                    
                    if (scannedCodes.Any())
                    {
                        Console.WriteLine("  Scanned codes:");
                        foreach (var code in scannedCodes.Distinct().Take(10))
                        {
                            Console.WriteLine($"    - {code}");
                        }
                    }
                }

                // Test beep
                Console.WriteLine();
                Console.Write("Test beep sound? (y/n): ");
                if (Console.ReadLine()?.ToLower() == "y")
                {
                    Console.WriteLine("Beeping...");
                    await scanner.BeepAsync(200);
                    await Task.Delay(300);
                    await scanner.BeepAsync(200);
                    Console.WriteLine("✓ Beep test completed");
                }

                Console.WriteLine();
                Console.WriteLine("✓ Barcode scanner test completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Scanner error: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
            }
        }
    }
}

