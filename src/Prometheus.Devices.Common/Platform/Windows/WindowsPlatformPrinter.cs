using System.Drawing;
using System.Drawing.Printing;
using Prometheus.Devices.Abstractions.Platform;

namespace Prometheus.Devices.Common.Platform.Windows
{
    public class WindowsPlatformPrinter : IPlatformPrinter
    {
        public Task<string[]> GetAvailablePrintersAsync(CancellationToken cancellationToken = default)
        {
            var printers = PrinterSettings.InstalledPrinters.Cast<string>().ToArray();
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
                    e.Graphics.DrawString(text, new Font("Arial", 12), Brushes.Black, e.MarginBounds.Left, e.MarginBounds.Top);
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
            var ext = Path.GetExtension(filePath).ToLower();

            await Task.Run(() =>
            {
                var printDoc = new PrintDocument();
                printDoc.PrinterSettings.PrinterName = printerName;
                printDoc.DocumentName = Path.GetFileName(filePath);

                if (ext == ".txt")
                {
                    var text = File.ReadAllText(filePath);
                    printDoc.PrintPage += (s, e) => e.Graphics.DrawString(text, new Font("Courier New", 10), Brushes.Black, e.MarginBounds);
                    printDoc.Print();
                }
                else if (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp")
                {
                    using var image = Image.FromFile(filePath);
                    printDoc.PrintPage += (s, e) => e.Graphics.DrawImage(image, e.MarginBounds);
                    printDoc.Print();
                }
                else
                {
                    throw new NotSupportedException($"File format {ext} not supported");
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

