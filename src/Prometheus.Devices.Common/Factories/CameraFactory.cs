using OpenCvSharp;
using Prometheus.Devices.Core.Interfaces;
using Prometheus.Devices.Cameras;

namespace Prometheus.Devices.Common.Factories
{
    /// <summary>
    /// Factory for creating and enumerating camera devices
    /// Cross-platform: Windows, Linux, macOS
    /// </summary>
    public static class CameraFactory
    {
        // Camera enumeration constants
        private const int DefaultMaxCameraProbe = 32;           // Safety limit for auto-detection
        private const int QuickCameraProbe = 3;                 // Fast scan (0-2): typical laptop/desktop
        private const int StandardCameraProbe = 10;             // Standard scan (0-9): most systems
        private const int ExtensiveCameraProbe = 16;            // Extensive scan (0-15): multi-camera systems
        private const int ConsecutiveFailuresToStop = 3;        // Stop after N consecutive failures

        // ============= LOCAL CAMERAS =============

        /// <summary>
        /// Create local camera by index (e.g., 0 for /dev/video0 on Linux, first camera on Windows)
        /// </summary>
        /// <param name="index">Camera index (default: 0)</param>
        /// <param name="name">Device name (optional)</param>
        public static ICamera CreateLocal(int index = 0, string name = null)
        {
            return new LocalCamera(index, deviceName: name ?? $"Local Camera #{index}");
        }

        /// <summary>
        /// Check if specific camera index is available (with error suppression)
        /// </summary>
        public static bool IsAvailable(int index)
        {
            var originalError = Console.Error;
            try
            {
                Console.SetError(System.IO.TextWriter.Null);
                using var cap = new VideoCapture(index);
                return cap.IsOpened();
            }
            catch
            {
                return false;
            }
            finally
            {
                Console.SetError(originalError);
            }
        }

        // ============= IP CAMERAS =============

        /// <summary>
        /// Create IP camera (RTSP/HTTP stream)
        /// </summary>
        /// <param name="ipAddress">Camera IP address or hostname</param>
        /// <param name="port">Camera port (default: 8080)</param>
        /// <param name="name">Device name (optional)</param>
        public static ICamera CreateIp(string ipAddress, int port = 8080, string name = null)
        {
            return IpCamera.Create(ipAddress, port, name);
        }

        // ============= USB CAMERAS =============

        /// <summary>
        /// Create USB camera by VID/PID (direct USB communication, not video capture)
        /// </summary>
        /// <param name="vendorId">USB Vendor ID</param>
        /// <param name="productId">USB Product ID</param>
        /// <param name="name">Device name (optional)</param>
        public static ICamera CreateUsb(int vendorId, int productId, string name = null)
        {
            return UsbCamera.Create(vendorId, productId, name);
        }

        // ============= ENUMERATION =============

        /// <summary>
        /// Enumerate available local camera indices with smart auto-stopping
        /// </summary>
        /// <param name="maxProbe">
        /// Maximum index to probe. Use null for auto-detection (stops after 3 consecutive failures).
        /// Common values: 3 (quick), 10 (standard), 16 (extensive)
        /// </param>
        /// <returns>Array of available camera indices</returns>
        public static int[] EnumerateIndices(int? maxProbe = null)
        {
            // Smart default: auto-detect with safety limit
            int actualMaxProbe = maxProbe ?? DefaultMaxCameraProbe;
            var indices = new List<int>();
            int consecutiveFailures = 0;
            
            // Suppress OpenCV errors during enumeration
            // OpenCV writes errors to stderr for non-existent indices - we can't catch them
            var originalError = Console.Error;
            
            try
            {
                Console.SetError(System.IO.TextWriter.Null);
                
                for (var i = 0; i < actualMaxProbe; i++)
                {
                    try
                    {
                        using var cap = new VideoCapture(i);
                        if (cap.IsOpened())
                        {
                            indices.Add(i);
                            consecutiveFailures = 0;  // Reset counter on success
                            
                            // Restore stderr after first camera found
                            if (indices.Count == 1)
                                Console.SetError(originalError);
                        }
                        else
                        {
                            consecutiveFailures++;
                            
                            // Smart stopping: if we found at least one camera
                            // and then failed N times in a row, stop scanning
                            if (indices.Count > 0 && consecutiveFailures >= ConsecutiveFailuresToStop)
                                break;
                        }
                    }
                    catch
                    {
                        consecutiveFailures++;
                        
                        // Also stop on exceptions if we have cameras
                        if (indices.Count > 0 && consecutiveFailures >= ConsecutiveFailuresToStop)
                            break;
                    }
                }
            }
            finally
            {
                // Always restore stderr
                Console.SetError(originalError);
            }
            
            return [.. indices];
        }

        /// <summary>
        /// Quick camera scan (0-2): typical laptop/desktop with built-in camera
        /// </summary>
        public static int[] EnumerateQuick() => EnumerateIndices(QuickCameraProbe);
        
        /// <summary>
        /// Standard camera scan (0-9): most development systems
        /// </summary>
        public static int[] EnumerateStandard() => EnumerateIndices(StandardCameraProbe);
        
        /// <summary>
        /// Extensive camera scan (0-15): systems with multiple cameras or capture cards
        /// </summary>
        public static int[] EnumerateExtensive() => EnumerateIndices(ExtensiveCameraProbe);
        
        /// <summary>
        /// Auto-detect cameras with smart stopping (stops after 3 consecutive failures, max 32)
        /// Recommended for production: minimal console errors
        /// </summary>
        public static int[] EnumerateAuto() => EnumerateIndices(null);

        // ============= HELPERS =============

        /// <summary>
        /// Get camera count (uses quick enumeration)
        /// </summary>
        public static int GetCount() => EnumerateQuick().Length;

        /// <summary>
        /// Check if any cameras are available
        /// </summary>
        public static bool HasCameras() => IsAvailable(0);
    }
}

