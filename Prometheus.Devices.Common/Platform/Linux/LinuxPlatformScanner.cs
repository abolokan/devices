using System.Diagnostics;
using Prometheus.Devices.Core.Interfaces;
using Prometheus.Devices.Core.Platform;

namespace Prometheus.Devices.Common.Platform.Linux
{
    public class LinuxPlatformScanner : IPlatformScanner
    {
        public async Task<string[]> GetAvailableScannersAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await RunCommandAsync("scanimage", "-L", cancellationToken);
                var scanners = result
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Where(line => line.StartsWith("device"))
                    .Select(line =>
                    {
                        var startIdx = line.IndexOf('`');
                        var endIdx = line.IndexOf('\'');
                        if (startIdx >= 0 && endIdx > startIdx)
                            return line.Substring(startIdx + 1, endIdx - startIdx - 1);
                        return null;
                    })
                    .Where(name => !string.IsNullOrEmpty(name))
                    .ToArray();
                return scanners;
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        public async Task<ScannedImage> ScanAsync(string scannerName, ScannerSettings settings, CancellationToken cancellationToken = default)
        {
            var tempFile = Path.GetTempFileName() + ".jpg";
            try
            {
                var colorMode = settings.ColorMode switch
                {
                    ScanColorMode.BlackAndWhite => "Lineart",
                    ScanColorMode.Grayscale => "Gray",
                    _ => "Color"
                };

                var arguments = $"-d \"{scannerName}\" --resolution {settings.Resolution} --mode {colorMode} --format jpeg > {tempFile}";
                await RunCommandAsync("/bin/bash", $"-c \"scanimage {arguments}\"", cancellationToken);

                var data = await File.ReadAllBytesAsync(tempFile, cancellationToken);
                return new ScannedImage
                {
                    Data = data,
                    Width = 0,
                    Height = 0,
                    Resolution = settings.Resolution,
                    ColorMode = settings.ColorMode,
                    Format = ScanFormat.JPEG,
                    Timestamp = DateTime.Now
                };
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        public async Task<bool> IsScannerAvailableAsync(string scannerName, CancellationToken cancellationToken = default)
        {
            var scanners = await GetAvailableScannersAsync(cancellationToken);
            return scanners.Contains(scannerName);
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
            await process.WaitForExitAsync(cancellationToken);
            return output;
        }
    }
}

