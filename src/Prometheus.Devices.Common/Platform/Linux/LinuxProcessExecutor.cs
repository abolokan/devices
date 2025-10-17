using System.Diagnostics;

namespace Prometheus.Devices.Common.Platform.Linux
{
    /// <summary>
    /// Helper class for executing Linux commands
    /// </summary>
    internal static class LinuxProcessExecutor
    {
        public static async Task<string> RunCommandAsync(string command, string arguments, CancellationToken cancellationToken = default)
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

