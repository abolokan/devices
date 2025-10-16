using Prometheus.Devices.Core.Interfaces;

namespace Prometheus.Devices.Core.Platform
{
    /// <summary>
    /// Platform-specific interface for scanning
    /// </summary>
    public interface IPlatformScanner
    {
        /// <summary>
        /// Get list of available scanners in the system
        /// </summary>
        Task<string[]> GetAvailableScannersAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Scan image
        /// </summary>
        Task<ScannedImage> ScanAsync(string scannerName, ScannerSettings settings, CancellationToken cancellationToken = default);

        /// <summary>
        /// Check scanner availability
        /// </summary>
        Task<bool> IsScannerAvailableAsync(string scannerName, CancellationToken cancellationToken = default);
    }
}
