using Prometheus.Devices.Abstractions.Interfaces;
using Prometheus.Devices.Abstractions.Drivers;
using Prometheus.Devices.Connections;
using Prometheus.Devices.Infrastructure.Extensions;
using Prometheus.Devices.Common.Configuration;
using Prometheus.Devices.Common.Factories;
using System.Text;

namespace Prometheus.Devices.Test.App.Tests
{
    /// <summary>
    /// Printer tests (ESC/POS, Network, Office)
    /// </summary>
    public static class PrinterTests
    {
        /// <summary>
        /// Test ESC/POS printer (Bixolon BK3-31) via TCP/IP
        /// </summary>
        public static async Task TestEscPosPrinterAsync(IDeviceManager deviceManager)
        {
            Console.WriteLine();
            Console.WriteLine("=== PRINTER TEST (ESC/POS) ===");

            var profilePath = Path.Combine(AppContext.BaseDirectory, "profiles", "printer.profile.json");
            if (!File.Exists(profilePath))
            {
                Console.WriteLine($"Profile not found: {profilePath}");
                return;
            }

            var profile = ProfileLoader.LoadPrinterProfile(profilePath);
            IPrinterDriver driver = PrinterFactory.ResolveDriver(profile);

            Console.Write("Enter printer IP: ");
            var ip = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(ip))
            {
                Console.WriteLine("IP address is required.");
                return;
            }

            Console.Write("Enter printer port (press Enter for 9100 - RAW printing): ");
            var portInput = Console.ReadLine();
            int port = 9100; 
            if (!string.IsNullOrWhiteSpace(portInput) && int.TryParse(portInput, out int parsedPort))
            {
                port = parsedPort;
            }

            var connection = new TcpConnection(ip, port);

            var printer = PrinterFactory.CreateDriver(
                connection,
                profile,
                driver,
                deviceName: $"{profile.Manufacturer} {profile.Model}");

            // Register device in DeviceManager
            deviceManager.RegisterDevice(printer);

            try
            {
                await printer.ConnectAsync();
                await printer.InitializeAsync();

                Console.WriteLine($"✓ Connected to {profile.Manufacturer} {profile.Model}");
                Console.WriteLine("Sending print job...");

                var printContent = BuildPrintContent(
                    "========================================",
                    $"  {profile.Manufacturer} {profile.Model}",
                    "  ESC/POS Test Print",
                    "========================================",
                    "",
                    "Cyrillic: Привет, мир!",
                    "Latin: Hello, World!",
                    "Numbers: 1234567890",
                    "",
                    "Date/Time: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"),
                    "",
                    "----------------------------------------",
                    $"Code page: {profile.DefaultCodepage} (ESC t {profile.EscPosCodepage})",
                    $"Cut: {(profile.SupportsCut ? "Yes" : "No")}",
                    "========================================"
                );

                await printer.PrintTextAsync(printContent);

                Console.WriteLine("✓ Print job sent. Check the receipt on printer.");
                Console.WriteLine($"✓ Device registered in DeviceManager with ID: {printer.DeviceId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Print error: {ex.Message}");
            }
        }

        /// <summary>
        /// Test Office printer (Windows/Linux via OS print spooler)
        /// </summary>
        public static async Task TestOfficePrinterAsync(IDeviceManager deviceManager)
        {
            Console.WriteLine();
            Console.WriteLine("=== OFFICE PRINTER TEST ===");

            Console.WriteLine("Searching for available printers...");
            var printers = await PrinterFactory.GetAvailableOfficePrintersAsync();

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

            IPrinter printer = PrinterFactory.CreateOffice(selectedPrinter);

            // Register device in DeviceManager
            deviceManager.RegisterDevice(printer);

            try
            {
                Console.WriteLine("Connecting to printer...");
                await printer.ConnectAsync();
                await printer.InitializeAsync();

                var info = await printer.GetDeviceInfoAsync();
                Console.WriteLine($"✓ Connected to {info.DeviceName}");
                Console.WriteLine($"  Manufacturer: {info.Manufacturer}");
                Console.WriteLine($"  Model: {info.Model}");

                Console.WriteLine();
                Console.WriteLine("Sending print job...");

                var printContent = BuildPrintContent(
                    "========================================",
                    $"  {selectedPrinter}",
                    "  Office Printer Test",
                    "========================================",
                    "",
                    "Cyrillic: Привет, мир!",
                    "Latin: Hello, World!",
                    "Numbers: 1234567890",
                    "",
                    "Date/Time: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"),
                    "",
                    "========================================"
                );

                await printer.PrintTextAsync(printContent);

                Console.WriteLine("✓ Print job sent. Check the printer output.");
                Console.WriteLine($"✓ Device registered in DeviceManager with ID: {printer.DeviceId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Print error: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Build print content from multiple lines
        /// </summary>
        private static string BuildPrintContent(params string[] lines)
        {
            var builder = new StringBuilder();
            foreach (var line in lines)
            {
                builder.AppendLine(line);
            }
            return builder.ToString();
        }
    }
}

