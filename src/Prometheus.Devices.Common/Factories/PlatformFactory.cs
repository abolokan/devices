using System.Runtime.InteropServices;
using Prometheus.Devices.Core.Platform;
using Prometheus.Devices.Common.Platform.Windows;
using Prometheus.Devices.Common.Platform.Linux;

namespace Prometheus.Devices.Common.Factories
{
    /// <summary>
    /// Factory for platform-specific implementations
    /// Detects OS and returns appropriate platform wrapper
    /// </summary>
    public static class PlatformFactory
    {
        // ============= PLATFORM PRINTERS =============

        /// <summary>
        /// Get platform-specific printer implementation
        /// Windows: WindowsPlatformPrinter (uses PrintDocument)
        /// Linux/macOS: LinuxPlatformPrinter (uses CUPS lpr)
        /// </summary>
        public static IPlatformPrinter GetPrinter()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return new WindowsPlatformPrinter();
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return new LinuxPlatformPrinter();
            else
                throw new PlatformNotSupportedException($"Platform not supported: {RuntimeInformation.OSDescription}");
        }

        // ============= PLATFORM SCANNERS =============

        /// <summary>
        /// Get platform-specific scanner implementation
        /// Windows: WindowsPlatformScanner (uses TWAIN)
        /// Linux/macOS: LinuxPlatformScanner (uses SANE)
        /// </summary>
        public static IPlatformScanner GetScanner()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return new WindowsPlatformScanner();
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return new LinuxPlatformScanner();
            else
                throw new PlatformNotSupportedException($"Platform not supported: {RuntimeInformation.OSDescription}");
        }

        // ============= PLATFORM DETECTION =============

        /// <summary>
        /// Check if running on Windows
        /// </summary>
        public static bool IsWindows() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        /// <summary>
        /// Check if running on Linux
        /// </summary>
        public static bool IsLinux() => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

        /// <summary>
        /// Check if running on macOS
        /// </summary>
        public static bool IsMacOS() => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        /// <summary>
        /// Get OS name (Windows, Linux, OSX, Unknown)
        /// </summary>
        public static string GetOSName()
        {
            if (IsWindows()) return "Windows";
            if (IsLinux()) return "Linux";
            if (IsMacOS()) return "macOS";
            return "Unknown";
        }

        /// <summary>
        /// Get detailed OS description
        /// </summary>
        public static string GetOSDescription() => RuntimeInformation.OSDescription;
    }
}

