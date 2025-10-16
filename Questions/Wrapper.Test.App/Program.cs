using Prometheus.Devices.Core.Interfaces;
using Prometheus.Devices.Core.Drivers;
using Prometheus.Devices.Core.Connections;
using Prometheus.Devices.Common.Configuration;
using Prometheus.Devices.Common.Factories;

class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("========================================");
        Console.WriteLine("  Device Wrappers Test Application");
        Console.WriteLine("========================================");
        Console.WriteLine();
        Console.WriteLine("Select device:");
        Console.WriteLine("1. Printer (Bixolon BK3-31 - ESC/POS)");
        Console.WriteLine("2. Camera (local camera)");
        Console.WriteLine("3. Office Printer (Samsung SCX-4200 - Windows/Linux)");
        Console.WriteLine("4. Scanner (Samsung SCX-4200 - Windows/Linux)");
        Console.Write("Your choice: ");
        
        var choice = Console.ReadLine();
        
        switch (choice)
        {
            case "1":
                await TestPrinterAsync();
                break;
            case "2":
                await TestCameraAsync();
                break;
            case "3":
                await TestOfficePrinterAsync();
                break;
            case "4":
                await TestScannerAsync();
                break;
            default:
                Console.WriteLine("Invalid choice. Exiting.");
                break;
        }
    }

    private static async Task TestPrinterAsync()
    {
        Console.WriteLine();
        Console.WriteLine("=== PRINTER TEST ===");
        
        var profilePath = Path.Combine(AppContext.BaseDirectory, "printer.profile.json");
        if (!File.Exists(profilePath))
        {
            Console.WriteLine($"Profile not found: {profilePath}");
            return;
        }

        var profile = ProfileLoader.LoadPrinterProfile(profilePath);
        IPrinterDriver driver = DeviceFactory.ResolvePrinterDriver(profile);

        Console.Write("Enter printer IP (press Enter for 192.168.1.50): ");
        var ip = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(ip)) ip = "192.168.1.50";
        
        var connection = new TcpConnection(ip, 9100);

        var printer = DeviceFactory.CreateDriverPrinter(
            connection,
            profile,
            driver,
            deviceName: $"{profile.Manufacturer} {profile.Model}");

        try
        {
            await printer.ConnectAsync();
            await printer.InitializeAsync();

            Console.WriteLine($"Connected to {profile.Manufacturer} {profile.Model}");
            Console.WriteLine("Sending print job...");
            
            await printer.PrintTextAsync("========================================");
            await printer.PrintTextAsync($"  {profile.Manufacturer} {profile.Model}");
            await printer.PrintTextAsync("  ESC/POS Test Print");
            await printer.PrintTextAsync("========================================");
            await printer.PrintTextAsync("");
            await printer.PrintTextAsync("Cyrillic: Привет, мир!");
            await printer.PrintTextAsync("Latin: Hello, World!");
            await printer.PrintTextAsync("Numbers: 1234567890");
            await printer.PrintTextAsync("");
            await printer.PrintTextAsync("Date/Time: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
            await printer.PrintTextAsync("");
            await printer.PrintTextAsync("----------------------------------------");
            await printer.PrintTextAsync($"Code page: {profile.DefaultCodepage} (ESC t {profile.EscPosCodepage})");
            await printer.PrintTextAsync($"Cut: {(profile.SupportsCut ? "Yes" : "No")}");
            await printer.PrintTextAsync("========================================");

            Console.WriteLine("✓ Print job sent. Check the receipt on printer.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Print error: {ex.Message}");
        }
        finally
        {
            await printer.DisconnectAsync();
            printer.Dispose();
        }
    }

    private static async Task TestCameraAsync()
    {
        Console.WriteLine();
        Console.WriteLine("=== CAMERA TEST ===");
        
        Console.WriteLine("Searching for available cameras...");
        var cameraIndices = DeviceFactory.EnumerateLocalCameraIndices(10);
        
        if (cameraIndices.Length == 0)
        {
            Console.WriteLine("No cameras found.");
            return;
        }

        Console.WriteLine($"Found cameras: {cameraIndices.Length}");
        for (int i = 0; i < cameraIndices.Length; i++)
        {
            Console.WriteLine($"  {i + 1}. Camera #{cameraIndices[i]} (index {cameraIndices[i]})");
        }
        
        Console.WriteLine();
        Console.Write($"Select camera (1-{cameraIndices.Length}): ");
        if (!int.TryParse(Console.ReadLine(), out int selection) || selection < 1 || selection > cameraIndices.Length)
        {
            Console.WriteLine("Invalid selection.");
            return;
        }

        var selectedIndex = cameraIndices[selection - 1];
        Console.WriteLine($"Selected camera #{selectedIndex}");

        ICamera camera = DeviceFactory.CreateLocalCamera(selectedIndex, $"Local Camera #{selectedIndex}");

        try
        {
            Console.WriteLine("Connecting to camera...");
            await camera.ConnectAsync();
            await camera.InitializeAsync();

            var info = await camera.GetDeviceInfoAsync();
            Console.WriteLine($"✓ Connected to {info.DeviceName}");
            Console.WriteLine($"  Manufacturer: {info.Manufacturer}");
            Console.WriteLine($"  Model: {info.Model}");
            
            var resolutions = await camera.GetSupportedResolutionsAsync();
            Console.WriteLine($"  Supported resolutions: {string.Join(", ", resolutions.Select(r => r.ToString()))}");

            Console.WriteLine();
            Console.WriteLine("Capturing frame...");
            
            var frame = await camera.CaptureFrameAsync();
            
            var filename = $"camera_{selectedIndex}_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
            var filepath = Path.Combine(AppContext.BaseDirectory, filename);
            await camera.SaveFrameAsync(frame, filepath);

            Console.WriteLine($"✓ Frame captured and saved:");
            Console.WriteLine($"  File: {filename}");
            Console.WriteLine($"  Path: {filepath}");
            Console.WriteLine($"  Resolution: {frame.Resolution}");
            Console.WriteLine($"  Format: {frame.Format}");
            Console.WriteLine($"  Size: {frame.Data.Length / 1024} KB");
            Console.WriteLine($"  Time: {frame.Timestamp:dd.MM.yyyy HH:mm:ss}");
            Console.WriteLine($"  Frame number: {frame.FrameNumber}");

            Console.WriteLine();
            Console.Write("Start video stream for 5 seconds? (y/n): ");
            if (Console.ReadLine()?.ToLower() == "y")
            {
                Console.WriteLine("Starting stream...");
                int frameCount = 0;
                camera.FrameCaptured += (s, e) => {
                    frameCount++;
                    Console.WriteLine($"  Received frame #{e.Frame.FrameNumber}, size: {e.Frame.Data.Length / 1024} KB");
                };

                await camera.StartStreamingAsync();
                await Task.Delay(5000);
                await camera.StopStreamingAsync();

                Console.WriteLine($"✓ Stream stopped. Captured frames: {frameCount}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Camera error: {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
        }
        finally
        {
            await camera.DisconnectAsync();
            camera.Dispose();
        }
    }

    private static async Task TestOfficePrinterAsync()
    {
        Console.WriteLine();
        Console.WriteLine("=== OFFICE PRINTER TEST ===");
        
        Console.WriteLine("Searching for available printers...");
        var printers = await DeviceFactory.GetAvailableOfficePrintersAsync();
        
        if (printers.Length == 0)
        {
            Console.WriteLine("No printers found in system.");
            return;
        }

        Console.WriteLine($"Found printers: {printers.Length}");
        for (int i = 0; i < printers.Length; i++)
        {
            Console.WriteLine($"  {i + 1}. {printers[i]}");
        }
        
        Console.WriteLine();
        Console.Write($"Select printer (1-{printers.Length}): ");
        if (!int.TryParse(Console.ReadLine(), out int selection) || selection < 1 || selection > printers.Length)
        {
            Console.WriteLine("Invalid selection.");
            return;
        }

        var selectedPrinter = printers[selection - 1];
        Console.WriteLine($"Selected printer: {selectedPrinter}");

        IPrinter printer = DeviceFactory.CreateOfficePrinter(selectedPrinter);

        try
        {
            Console.WriteLine("Connecting to printer...");
            await printer.ConnectAsync();
            await printer.InitializeAsync();

            var info = await printer.GetDeviceInfoAsync();
            Console.WriteLine($"✓ Connected to {info.DeviceName}");
            Console.WriteLine($"  Type: {info.DeviceType}");
            Console.WriteLine($"  Model: {info.Model}");

            Console.WriteLine();
            Console.WriteLine("Printing test page...");
            
            var text = "========================================\n" +
                      "  Office Printer Test\n" +
                      $"  Printer: {selectedPrinter}\n" +
                      "========================================\n" +
                      "\n" +
                      "This is a test print from DeviceWrappers\n" +
                      "Cross-platform office printing\n" +
                      $"Date/Time: {DateTime.Now:dd.MM.yyyy HH:mm:ss}\n" +
                      "\n" +
                      "Cyrillic: Привет, мир!\n" +
                      "Latin: Hello, World!\n" +
                      "Numbers: 1234567890\n" +
                      "========================================";

            var job = await printer.PrintTextAsync(text);
            
            Console.WriteLine($"✓ Print job sent:");
            Console.WriteLine($"  Job ID: {job.JobId}");
            Console.WriteLine($"  Status: {job.Status}");
            Console.WriteLine($"  Submitted: {job.SubmittedAt:dd.MM.yyyy HH:mm:ss}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Printer error: {ex.Message}");
        }
        finally
        {
            await printer.DisconnectAsync();
            printer.Dispose();
        }
    }

    private static async Task TestScannerAsync()
    {
        Console.WriteLine();
        Console.WriteLine("=== SCANNER TEST ===");
        
        Console.WriteLine("Searching for available scanners...");
        var scanners = await DeviceFactory.GetAvailableScannersAsync();
        
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

        IScanner scanner = DeviceFactory.CreateOfficeScanner(selectedScanner);

        try
        {
            Console.WriteLine("Connecting to scanner...");
            await scanner.ConnectAsync();
            await scanner.InitializeAsync();

            var info = await scanner.GetDeviceInfoAsync();
            Console.WriteLine($"✓ Connected to {info.DeviceName}");
            Console.WriteLine($"  Type: {info.DeviceType}");
            Console.WriteLine($"  Model: {info.Model}");
            
            var resolutions = await scanner.GetSupportedResolutionsAsync();
            Console.WriteLine($"  Supported resolutions: {string.Join(", ", resolutions)} DPI");

            Console.WriteLine();
            Console.WriteLine("Scanning...");
            Console.WriteLine("(Place document on scanner and press Enter)");
            Console.ReadLine();

            scanner.Settings.Resolution = 300;
            scanner.Settings.ColorMode = ScanColorMode.Color;
            
            var image = await scanner.ScanAsync();
            
            var filename = $"scan_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
            var filepath = Path.Combine(AppContext.BaseDirectory, filename);
            await scanner.SaveImageAsync(image, filepath);

            Console.WriteLine($"✓ Scan completed and saved:");
            Console.WriteLine($"  File: {filename}");
            Console.WriteLine($"  Path: {filepath}");
            Console.WriteLine($"  Resolution: {image.Resolution} DPI");
            Console.WriteLine($"  Color mode: {image.ColorMode}");
            Console.WriteLine($"  Format: {image.Format}");
            Console.WriteLine($"  Size: {image.Data.Length / 1024} KB");
            Console.WriteLine($"  Time: {image.Timestamp:dd.MM.yyyy HH:mm:ss}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Scanner error: {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
        }
        finally
        {
            await scanner.DisconnectAsync();
            scanner.Dispose();
        }
    }
}