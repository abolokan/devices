using System.Drawing;
using System.Drawing.Printing;
using Prometheus.Devices.Core.Platform;

namespace DeviceWrappers.Platform.Windows
{
    /// <summary>
    /// Windows-реализация печати через System.Drawing.Printing
    /// </summary>
    public class WindowsPlatformPrinter : IPlatformPrinter
    {
        public Task<string[]> GetAvailablePrintersAsync(CancellationToken cancellationToken = default)
        {
            var printers = PrinterSettings.InstalledPrinters
                .Cast<string>()
                .ToArray();
            return Task.FromResult(printers);
        }

        public async Task<string> PrintTextAsync(string printerName, string text, CancellationToken cancellationToken = default)
        {
            var jobId = Guid.NewGuid().ToString();

            await Task.Run(() =>
            {
                var printDoc = new PrintDocument();
                printDoc.PrinterSettings.PrinterName = printerName;
                printDoc.DocumentName = $"TextPrint_{jobId}";

                printDoc.PrintPage += (sender, e) =>
                {
                    var font = new Font("Arial", 12);
                    var brush = Brushes.Black;
                    var leftMargin = e.MarginBounds.Left;
                    var topMargin = e.MarginBounds.Top;

                    e.Graphics.DrawString(text, font, brush, leftMargin, topMargin);
                };

                printDoc.Print();
            }, cancellationToken);

            return jobId;
        }

        public async Task<string> PrintFileAsync(string printerName, string filePath, CancellationToken cancellationToken = default)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("File not found", filePath);

            var jobId = Guid.NewGuid().ToString();
            var extension = Path.GetExtension(filePath).ToLower();

            await Task.Run(() =>
            {
                var printDoc = new PrintDocument();
                printDoc.PrinterSettings.PrinterName = printerName;
                printDoc.DocumentName = Path.GetFileName(filePath);

                if (extension == ".txt")
                {
                    var text = File.ReadAllText(filePath);
                    printDoc.PrintPage += (sender, e) =>
                    {
                        var font = new Font("Courier New", 10);
                        e.Graphics.DrawString(text, font, Brushes.Black, e.MarginBounds);
                    };
                    printDoc.Print();
                }
                else if (extension == ".jpg" || extension == ".jpeg" || extension == ".png" || extension == ".bmp")
                {
                    using var image = Image.FromFile(filePath);
                    printDoc.PrintPage += (sender, e) =>
                    {
                        e.Graphics.DrawImage(image, e.MarginBounds);
                    };
                    printDoc.Print();
                }
                else
                {
                    throw new NotSupportedException($"File format {extension} not supported. Use TXT or image files.");
                }
            }, cancellationToken);

            return jobId;
        }

        public Task<bool> IsPrinterAvailableAsync(string printerName, CancellationToken cancellationToken = default)
        {
            try
            {
                var settings = new PrinterSettings { PrinterName = printerName };
                return Task.FromResult(settings.IsValid);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }
    }
}

