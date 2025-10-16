using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DeviceWrappers.Core.Interfaces;
using DeviceWrappers.Core.Platform;

namespace DeviceWrappers.Platform.Linux
{
    /// <summary>
    /// Linux-реализация сканирования через SANE (Scanner Access Now Easy)
    /// </summary>
    public class LinuxPlatformScanner : IPlatformScanner
    {
        public async Task<string[]> GetAvailableScannersAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // scanimage -L
                var result = await RunCommandAsync("scanimage", "-L", cancellationToken);
                var scanners = result
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Where(line => line.StartsWith("device"))
                    .Select(line =>
                    {
                        // "device `samsung:usb:0x04e8:0x3413' is a Samsung SCX-4200 Series USB flatbed scanner"
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
                // scanimage -d "scanner_name" --resolution 300 --format jpeg > output.jpg
                var colorMode = settings.ColorMode switch
                {
                    ScanColorMode.BlackAndWhite => "Lineart",
                    ScanColorMode.Grayscale => "Gray",
                    ScanColorMode.Color => "Color",
                    _ => "Color"
                };

                var arguments = $"-d \"{scannerName}\" " +
                               $"--resolution {settings.Resolution} " +
                               $"--mode {colorMode} " +
                               $"--format jpeg " +
                               $"> {tempFile}";

                await RunCommandAsync("/bin/bash", $"-c \"scanimage {arguments}\"", cancellationToken);

                if (!File.Exists(tempFile) || new FileInfo(tempFile).Length == 0)
                    throw new InvalidOperationException("Scan failed: no output file generated");

                var data = await File.ReadAllBytesAsync(tempFile, cancellationToken);

                // Get image dimensions (simplified - in production use ImageSharp or similar)
                return new ScannedImage
                {
                    Data = data,
                    Width = 0, // Would need image library to get actual dimensions
                    Height = 0,
                    Resolution = settings.Resolution,
                    ColorMode = settings.ColorMode,
                    Format = ScanFormat.JPEG,
                    Timestamp = DateTime.Now
                };
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        public async Task<bool> IsScannerAvailableAsync(string scannerName, CancellationToken cancellationToken = default)
        {
            try
            {
                var scanners = await GetAvailableScannersAsync(cancellationToken);
                return scanners.Contains(scannerName);
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

            if (process.ExitCode != 0 && !string.IsNullOrEmpty(error))
                throw new InvalidOperationException($"Command failed: {command} {arguments}\nError: {error}");

            return output;
        }
    }
}

