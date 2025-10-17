using Prometheus.Devices.Core.Interfaces;
using DeviceWrappers.Devices.Scanner;

namespace Prometheus.Devices.Common.Factories
{
    /// <summary>
    /// Factory for creating scanner devices
    /// Supports: Office scanners (TWAIN on Windows, SANE on Linux)
    /// Cross-platform: Windows, Linux, macOS
    /// </summary>
    public static class ScannerFactory
    {
        // ============= OFFICE SCANNERS =============

        /// <summary>
        /// Create office scanner (OS-managed via TWAIN/SANE)
        /// Windows: TWAIN, Linux: SANE, macOS: TWAIN/SANE
        /// </summary>
        /// <param name="systemScannerName">System scanner name (TWAIN device name or SANE device string)</param>
        /// <param name="deviceName">Device name (optional)</param>
        public static IScanner CreateOffice(string systemScannerName, string? deviceName = null)
        {
            var platformScanner = PlatformFactory.GetScanner();
            var deviceId = $"OFFICE_SCANNER_{systemScannerName}";
            var name = deviceName ?? systemScannerName;
            return new OfficeScanner(deviceId, name, systemScannerName, platformScanner);
        }

        /// <summary>
        /// Get list of available office scanners from OS
        /// Windows: via TWAIN, Linux: via 'scanimage -L'
        /// </summary>
        public static async Task<string[]> GetAvailableScannersAsync()
        {
            var platformScanner = PlatformFactory.GetScanner();
            return await platformScanner.GetAvailableScannersAsync();
        }

        // ============= HELPERS =============

        /// <summary>
        /// Check if any scanners are available
        /// </summary>
        public static async Task<bool> HasScannersAsync()
        {
            var scanners = await GetAvailableScannersAsync();
            return scanners.Length > 0;
        }

        /// <summary>
        /// Get scanner count
        /// </summary>
        public static async Task<int> GetCountAsync()
        {
            var scanners = await GetAvailableScannersAsync();
            return scanners.Length;
        }
    }
}

