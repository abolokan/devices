using Prometheus.Devices.Core.Interfaces;
using Prometheus.Devices.Core.Platform;

namespace Prometheus.Devices.Common.Platform.Windows
{
    public class WindowsPlatformScanner : IPlatformScanner
    {
        public Task<string[]> GetAvailableScannersAsync(CancellationToken cancellationToken = default)
        {
            // Mock - для реальной работы требуется COM reference на WIA
            return Task.FromResult(new[] { "WIA Scanner (Mock - add COM reference)" });
        }

        public Task<ScannedImage> ScanAsync(string scannerName, ScannerSettings settings, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("WIA scanning requires COM reference to 'WIA' library");
        }

        public Task<bool> IsScannerAvailableAsync(string scannerName, CancellationToken cancellationToken = default)
        {
            var scanners = GetAvailableScannersAsync(cancellationToken).Result;
            return Task.FromResult(scanners.Contains(scannerName));
        }
    }
}

