using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Prometheus.Devices.Core.Extensions;
using Prometheus.Devices.Core.HealthChecks;
using Prometheus.Devices.Test.App.Tests;

namespace Prometheus.Devices.Test.App
{
    class Program
    {
        private static IServiceProvider _serviceProvider = null!;
        private static IDeviceManager _deviceManager = null!;

        public static async Task Main(string[] args)
        {
            // Setup Dependency Injection
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
            _deviceManager = _serviceProvider.GetRequiredService<IDeviceManager>();

            // Display menu
            Console.WriteLine("==============================================");
            Console.WriteLine("  Prometheus Devices Test Application");
            Console.WriteLine("==============================================");
            Console.WriteLine();
            Console.WriteLine("Select test:");
            Console.WriteLine("  0. Exit");
            Console.WriteLine("  1. Printer (ESC/POS - Bixolon BK3-31)");
            Console.WriteLine("  2. Camera (Local)");
            Console.WriteLine("  3. Printer (Office - Windows/Linux)");
            Console.WriteLine("  4. Scanner (Office - Windows/Linux)");
            Console.WriteLine("  5. Health Check (All registered devices)");
            Console.WriteLine("  6. Load devices from appsettings.json");
            Console.WriteLine();

            bool isRunning = true;
            while (isRunning)
            {
                Console.Write("Your choice: ");
                var choice = Console.ReadLine();

                switch (choice)
                {
                    case "0":
                    Console.WriteLine("Exiting...");
                    isRunning = false;
                    break;

                    case "1":
                    await PrinterTests.TestEscPosPrinterAsync(_deviceManager);
                    break;

                    case "2":
                    await CameraTests.TestLocalCameraAsync(_deviceManager);
                    break;

                    case "3":
                    await PrinterTests.TestOfficePrinterAsync(_deviceManager);
                    break;

                    case "4":
                    await ScannerTests.TestOfficeScannerAsync(_deviceManager);
                    break;

                    case "5":
                    await HealthCheckTests.TestHealthCheckAsync(_serviceProvider);
                    break;

                    case "6":
                    await LoadDevicesFromConfigAsync();
                    break;

                    default:
                    Console.WriteLine("Invalid choice. Try again.");
                    break;
                }

                if (isRunning)
                {
                    Console.WriteLine();
                    Console.WriteLine("---");
                    Console.WriteLine();
                }
            }

            Console.WriteLine();
            Console.WriteLine("Disconnecting all devices...");
            await _deviceManager.DisconnectAllAsync();
            Console.WriteLine("✓ Done.");
        }

        /// <summary>
        /// Configure Dependency Injection services
        /// </summary>
        private static void ConfigureServices(IServiceCollection services)
        {
            // Add configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true)
                .Build();

            services.AddSingleton<IConfiguration>(configuration);

            // Add Logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });

            // Register Prometheus.Devices via Extension
            services.AddPrometheusDevicesCore(configuration);

            // Add Health Checks
            services.AddHealthChecks()
                .AddDeviceHealthCheck("devices", failureStatus: HealthStatus.Degraded, tags: new[] { "devices", "hardware" });
        }

        /// <summary>
        /// Load devices from appsettings.json configuration
        /// </summary>
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
                Console.Write("Do you want to connect to all loaded devices? (y/n): ");
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
}
