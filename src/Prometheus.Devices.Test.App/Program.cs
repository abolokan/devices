using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Prometheus.Devices.Core.Interfaces;
using Prometheus.Devices.Core.Drivers;
using Prometheus.Devices.Core.Connections;
using Prometheus.Devices.Core.Extensions;
using Prometheus.Devices.Core.HealthChecks;
using Prometheus.Devices.Common.Configuration;
using Prometheus.Devices.Common.Factories;
using Prometheus.Devices.Test.App;

class Program
{
    private static IServiceProvider _serviceProvider = null!;
    private static IDeviceManager _deviceManager = null!;

    public static async Task Main(string[] args)
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
        _deviceManager = _serviceProvider.GetRequiredService<IDeviceManager>();

        Console.WriteLine("Select device:");
        Console.WriteLine("0. Exit");
        Console.WriteLine("1. Printer (Bixolon BK3-31 - ESC/POS)");
        Console.WriteLine("2. Camera (local)");
        Console.WriteLine("3. Office Printer (Windows/Linux)");
        Console.WriteLine("4. Scanner (Windows/Linux)");
        Console.WriteLine("5. Health Check all devices");
        Console.WriteLine("6. Load devices from appsettings.json");

        bool isRunning = true;

        while (isRunning)
        {
            Console.WriteLine("Your choice: ");

            var choice = Console.ReadLine();

            switch (choice)
            {
                case "0":
                isRunning = false;
                break;
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
                case "5":
                    await TestHealthCheckAsync();
                    break;
                case "6":
                    await LoadDevicesFromConfigAsync();
                    break;
                default:
                    Console.WriteLine("Invalid choice. Exiting.");
                    await Task.Delay(1000);
                    isRunning = false;
                    break;
            }
        }

        await _deviceManager.DisconnectAllAsync();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Add configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        services.AddSingleton<IConfiguration>(configuration);

        // Register Prometheus.Devices via Extension
        services.AddPrometheusDevicesCore(configuration);

        // Add Health Checks
        services.AddHealthChecks()
            .AddDeviceHealthCheck("devices", failureStatus: HealthStatus.Degraded, tags: new[] { "devices", "hardware" });
    }

    private static async Task TestHealthCheckAsync()
    {
        Console.WriteLine();
        Console.WriteLine("=== HEALTH CHECK ALL DEVICES ===");
        Console.WriteLine();

        var devices = _deviceManager.GetAllDevices().ToList();
        Console.WriteLine($"Registered devices: {devices.Count}");

        if (devices.Count == 0)
        {
            Console.WriteLine("No devices registered. Run other device tests first.");
            return;
        }

        Console.WriteLine();
        Console.WriteLine("Device list:");
        foreach (var device in devices)
        {
            Console.WriteLine($"  - {device.DeviceName} [{device.DeviceId}]: {device.Status}");
        }

        Console.WriteLine();
        Console.WriteLine("Running Health Check...");

        var healthCheckService = _serviceProvider.GetRequiredService<HealthCheckService>();
        var result = await healthCheckService.CheckHealthAsync();

        Console.WriteLine();
        Console.WriteLine($"Overall status: {result.Status}");
        Console.WriteLine();
        Console.WriteLine("Detailed information:");

        foreach (var entry in result.Entries)
        {
            Console.WriteLine($"  [{entry.Key}]: {entry.Value.Status}");
            if (entry.Value.Description != null)
                Console.WriteLine($"    Description: {entry.Value.Description}");

            if (entry.Value.Data.Count > 0)
            {
                Console.WriteLine("    Data:");
                foreach (var data in entry.Value.Data)
                {
                    Console.WriteLine($"      {data.Key}: {data.Value}");
                }
            }

            if (entry.Value.Exception != null)
                Console.WriteLine($"    Error: {entry.Value.Exception.Message}");
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
        if (string.IsNullOrWhiteSpace(ip))
            ip = "192.168.1.50";

        var connection = new TcpConnection(ip, 9100);

        var printer = DeviceFactory.CreateDriverPrinter(
            connection,
            profile,
            driver,
            deviceName: $"{profile.Manufacturer} {profile.Model}");

        // Register device in DeviceManager
        _deviceManager.RegisterDevice(printer);

        try
        {
            await printer.ConnectAsync();
            await printer.InitializeAsync();

            Console.WriteLine($"✓ Connected to {profile.Manufacturer} {profile.Model}");
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
            Console.WriteLine($"✓ Device registered in DeviceManager with ID: {printer.DeviceId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Print error: {ex.Message}");
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

        // Register device in DeviceManager
        _deviceManager.RegisterDevice(camera);

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
            Console.WriteLine($"✓ Device registered in DeviceManager with ID: {camera.DeviceId}");

            Console.WriteLine();
            Console.Write("Start video stream for 5 seconds? (y/n): ");
            if (Console.ReadLine()?.ToLower() == "y")
            {
                Console.WriteLine("Starting stream...");
                int frameCount = 0;
                camera.FrameCaptured += (s, e) =>
                {
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

        // Register device in DeviceManager
        _deviceManager.RegisterDevice(printer);

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
            Console.WriteLine($"✓ Device registered in DeviceManager with ID: {printer.DeviceId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Printer error: {ex.Message}");
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

        // Register device in DeviceManager
        _deviceManager.RegisterDevice(scanner);

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
            Console.WriteLine($"✓ Device registered in DeviceManager with ID: {scanner.DeviceId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Scanner error: {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
        }
    }

    private static async Task LoadDevicesFromConfigAsync()
    {
        Console.WriteLine();
        Console.WriteLine("=== LOAD DEVICES FROM CONFIGURATION ===");
        Console.WriteLine();

        try
        {
            var configuration = _serviceProvider.GetRequiredService<IConfiguration>();
            
            Console.WriteLine("Loading devices from appsettings.json...");
            DeviceConfigurationExample.LoadDevicesFromConfiguration(configuration, _deviceManager);

            Console.WriteLine();
            Console.WriteLine("Do you want to connect to all loaded devices? (y/n): ");
            if (Console.ReadLine()?.ToLower() == "y")
            {
                await DeviceConfigurationExample.ConnectAllDevicesAsync(_deviceManager);
            }

            Console.WriteLine();
            Console.WriteLine("What would you like to test?");
            Console.WriteLine("1. Test all printers");
            Console.WriteLine("2. Capture from all cameras");
            Console.WriteLine("3. Skip");
            Console.Write("Your choice: ");

            var choice = Console.ReadLine();
            switch (choice)
            {
                case "1":
                    await DeviceConfigurationExample.TestAllPrintersAsync(_deviceManager);
                    break;
                case "2":
                    await DeviceConfigurationExample.CaptureFromAllCamerasAsync(_deviceManager);
                    break;
            }

            Console.WriteLine();
            Console.WriteLine("✓ Configuration example completed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Configuration error: {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
        }
    }
}