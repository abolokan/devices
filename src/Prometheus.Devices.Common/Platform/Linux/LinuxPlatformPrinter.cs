using Prometheus.Devices.Abstractions.Platform;

namespace Prometheus.Devices.Common.Platform.Linux
{
    public class LinuxPlatformPrinter : IPlatformPrinter
    {
        public async Task<string[]> GetAvailablePrintersAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await LinuxProcessExecutor.RunCommandAsync("lpstat", "-p", cancellationToken);
                var printers = result
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Where(line => line.StartsWith("printer"))
                    .Select(line => line.Split(' ')[1])
                    .Where(name => !string.IsNullOrEmpty(name))
                    .ToArray();
                return printers;
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        public async Task<string> PrintTextAsync(string printerName, string text, CancellationToken cancellationToken = default)
        {
            var tempFile = Path.GetTempFileName();
            await File.WriteAllTextAsync(tempFile, text, cancellationToken);
            try
            {
                var jobId = await LinuxProcessExecutor.RunCommandAsync("lp", $"-d {printerName} {tempFile}", cancellationToken);
                return jobId.Trim();
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        public async Task<string> PrintFileAsync(string printerName, string filePath, CancellationToken cancellationToken = default)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("File not found", filePath);
            var jobId = await LinuxProcessExecutor.RunCommandAsync("lp", $"-d {printerName} {filePath}", cancellationToken);
            return jobId.Trim();
        }

        public async Task<bool> IsPrinterAvailableAsync(string printerName, CancellationToken cancellationToken = default)
        {
            var printers = await GetAvailablePrintersAsync(cancellationToken);
            return printers.Contains(printerName);
        }
    }
}

