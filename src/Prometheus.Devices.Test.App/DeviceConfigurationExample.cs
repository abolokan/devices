using Microsoft.Extensions.Configuration;
using Prometheus.Devices.Core.Configuration;
using Prometheus.Devices.Core.Connections;
using Prometheus.Devices.Core.Extensions;
using Prometheus.Devices.Core.Interfaces;
using Prometheus.Devices.Common.Configuration;
using Prometheus.Devices.Common.Factories;

namespace Prometheus.Devices.Test.App
{
    /// <summary>
    /// Example of how to use appsettings.json to configure and create devices
    /// </summary>
    public static class DeviceConfigurationExample
    {
        /// <summary>
        /// Load and create all enabled devices from configuration
        /// </summary>
        public static void LoadDevicesFromConfiguration(
            IConfiguration configuration, 
            IDeviceManager deviceManager)
        {
            var options = new PrometheusDevicesOptions();
            configuration.GetSection("PrometheusDevices").Bind(options);

            // Load cameras
            foreach (var (key, cameraConfig) in options.Cameras)
            {
                if (!cameraConfig.Enabled) continue;

                try
                {
                    ICamera camera = cameraConfig.Type?.ToLower() switch
                    {
                        "local" => CameraFactory.CreateLocal(
                            cameraConfig.Index ?? 0, 
                            key),

                        "ip" => CameraFactory.CreateIp(
                            cameraConfig.IpAddress, 
                            cameraConfig.Port ?? 8080, 
                            key),

                        "usb" when cameraConfig.VendorId.HasValue && cameraConfig.ProductId.HasValue 
                            => CameraFactory.CreateUsb(
                                cameraConfig.VendorId.Value,
                                cameraConfig.ProductId.Value,
                                key),

                        _ => throw new InvalidOperationException($"Unknown camera type: {cameraConfig.Type}")
                    };

                    // Apply settings
                    camera.Settings.FrameRate = cameraConfig.FrameRate;
                    if (!string.IsNullOrEmpty(cameraConfig.Resolution))
                    {
                        var parts = cameraConfig.Resolution.Split('x');
                        if (parts.Length == 2 && int.TryParse(parts[0], out var width) && int.TryParse(parts[1], out var height))
                        {
                            camera.Settings.Resolution = new Resolution(width, height);
                        }
                    }

                    deviceManager.RegisterDevice(camera);
                    Console.WriteLine($"✓ Registered camera: {key} ({cameraConfig.Type})");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Failed to register camera {key}: {ex.Message}");
                }
            }

            // Load printers
            foreach (var (key, printerConfig) in options.Printers)
            {
                if (!printerConfig.Enabled) continue;

                try
                {
                    IPrinter printer = printerConfig.Type?.ToLower() switch
                    {
                        "driver" => CreateDriverPrinter(printerConfig, key),
                        
                        "office" => PrinterFactory.CreateOffice(
                            printerConfig.SystemPrinterName),

                        "network" => PrinterFactory.CreateNetwork(
                            printerConfig.IpAddress,
                            printerConfig.Port,
                            key),

                        "serial" => PrinterFactory.CreateSerial(
                            printerConfig.PortName,
                            printerConfig.BaudRate,
                            key),

                        "usb" when printerConfig.VendorId.HasValue && printerConfig.ProductId.HasValue
                            => PrinterFactory.CreateUsb(
                                printerConfig.VendorId.Value,
                                printerConfig.ProductId.Value,
                                key),

                        _ => throw new InvalidOperationException($"Unknown printer type: {printerConfig.Type}")
                    };

                    deviceManager.RegisterDevice(printer);
                    Console.WriteLine($"✓ Registered printer: {key} ({printerConfig.Type})");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Failed to register printer {key}: {ex.Message}");
                }
            }

            // Load scanners
            foreach (var (key, scannerConfig) in options.Scanners)
            {
                if (!scannerConfig.Enabled) continue;

                try
                {
                    var scanner = ScannerFactory.CreateOffice(scannerConfig.SystemScannerName);
                    scanner.Settings.Resolution = scannerConfig.Resolution;
                    scanner.Settings.ColorMode = scannerConfig.ColorMode?.ToLower() switch
                    {
                        "color" => ScanColorMode.Color,
                        "grayscale" => ScanColorMode.Grayscale,
                        "blackandwhite" => ScanColorMode.BlackAndWhite,
                        _ => ScanColorMode.Color
                    };

                    deviceManager.RegisterDevice(scanner);
                    Console.WriteLine($"✓ Registered scanner: {key}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Failed to register scanner {key}: {ex.Message}");
                }
            }
        }

        private static IPrinter CreateDriverPrinter(PrinterOptions config, string name)
        {
            var profilePath = Path.Combine(AppContext.BaseDirectory, config.ProfilePath);
            if (!File.Exists(profilePath))
                throw new FileNotFoundException($"Printer profile not found: {profilePath}");

            var profile = ProfileLoader.LoadPrinterProfile(profilePath);
            var driver = PrinterFactory.ResolveDriver(profile);
            var connection = new TcpConnection(config.IpAddress, config.Port);

            return PrinterFactory.CreateDriver(connection, profile, driver, deviceName: name);
        }

        /// <summary>
        /// Example: Connect to all devices
        /// </summary>
        public static async Task ConnectAllDevicesAsync(IDeviceManager deviceManager)
        {
            Console.WriteLine("\nConnecting to all registered devices...");
            
            var devices = deviceManager.GetAllDevices().ToList();
            Console.WriteLine($"Total devices: {devices.Count}");

            foreach (var device in devices)
            {
                try
                {
                    Console.Write($"  Connecting to {device.DeviceName}...");
                    await device.ConnectAsync();
                    await device.InitializeAsync();
                    Console.WriteLine(" ✓ Connected");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($" ✗ Failed: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Example: Print test on all printers
        /// </summary>
        public static async Task TestAllPrintersAsync(IDeviceManager deviceManager)
        {
            var printers = deviceManager.GetDevicesByType<IPrinter>().ToList();
            Console.WriteLine($"\nFound {printers.Count} printer(s)");

            foreach (var printer in printers)
            {
                try
                {
                    Console.WriteLine($"Printing test page on {printer.DeviceName}...");
                    await printer.PrintTextAsync("Test from configuration example");
                    await printer.PrintTextAsync($"Device: {printer.DeviceId}");
                    await printer.PrintTextAsync($"Time: {DateTime.Now}");
                    Console.WriteLine("  ✓ Print job sent");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  ✗ Failed: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Example: Capture from all cameras
        /// </summary>
        public static async Task CaptureFromAllCamerasAsync(IDeviceManager deviceManager)
        {
            var cameras = deviceManager.GetDevicesByType<ICamera>().ToList();
            Console.WriteLine($"\nFound {cameras.Count} camera(s)");

            foreach (var camera in cameras)
            {
                try
                {
                    Console.WriteLine($"Capturing from {camera.DeviceName}...");
                    var frame = await camera.CaptureFrameAsync();
                    var filename = $"{camera.DeviceId}_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
                    var filepath = Path.Combine(AppContext.BaseDirectory, filename);
                    await camera.SaveFrameAsync(frame, filepath);
                    Console.WriteLine($"  ✓ Saved: {filename}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  ✗ Failed: {ex.Message}");
                }
            }
        }
    }
}

