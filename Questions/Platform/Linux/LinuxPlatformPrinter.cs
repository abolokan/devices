using System;
using System.Diagnostics;
using DeviceWrappers.Core.Platform;

namespace DeviceWrappers.Platform.Linux
{
    /// <summary>
    /// Linux-реализация печати через CUPS (Common Unix Printing System)
    /// </summary>
    public class LinuxPlatformPrinter : IPlatformPrinter
    {
        public async Task<string[]> GetAvailablePrintersAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // lpstat -p -d
                var result = await RunCommandAsync("lpstat", "-p", cancellationToken);
                var printers = result
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Where(line => line.StartsWith("printer"))
                    .Select(line =>
                    {
                        // "printer Samsung_SCX4200 is idle..."
                        var parts = line.Split(' ');
                        return parts.Length > 1 ? parts[1] : null;
                    })
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
            // Create temp file
            var tempFile = Path.GetTempFileName();
            await File.WriteAllTextAsync(tempFile, text, cancellationToken);

            try
            {
                var jobId = await RunCommandAsync("lp", $"-d {printerName} {tempFile}", cancellationToken);
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

            // lp -d printer_name file_path
            var jobId = await RunCommandAsync("lp", $"-d {printerName} {filePath}", cancellationToken);
            return jobId.Trim();
        }

        public async Task<bool> IsPrinterAvailableAsync(string printerName, CancellationToken cancellationToken = default)
        {
            try
            {
                var printers = await GetAvailablePrintersAsync(cancellationToken);
                return printers.Contains(printerName);
            }
            catch
            {
                return false;
            }
        }

        private async Task<string> RunCommandAsync(string command, string arguments, CancellationToken cancellationToken = default)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
                throw new InvalidOperationException($"Command failed: {command} {arguments}\nError: {error}");

            return output;
        }
    }
}

