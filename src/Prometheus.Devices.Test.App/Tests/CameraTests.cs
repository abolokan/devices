using Prometheus.Devices.Core.Interfaces;
using Prometheus.Devices.Core.Extensions;
using Prometheus.Devices.Common.Factories;

namespace Prometheus.Devices.Test.App.Tests
{
    /// <summary>
    /// Camera tests (Local, IP, USB)
    /// </summary>
    public static class CameraTests
    {
        /// <summary>
        /// Test local camera (OpenCV VideoCapture)
        /// </summary>
        public static async Task TestLocalCameraAsync(IDeviceManager deviceManager)
        {
            Console.WriteLine();
            Console.WriteLine("=== CAMERA TEST ===");

            Console.WriteLine("Searching for available cameras...");
            Console.WriteLine("(Using smart auto-detection with minimal errors)");
            var cameraIndices = CameraFactory.EnumerateAuto();

            if (cameraIndices.Length == 0)
            {
                Console.WriteLine("No cameras found.");
                return;
            }

            Console.WriteLine($"Found cameras: {cameraIndices.Length}");
            for (int i = 0; i < cameraIndices.Length; i++)
            {
                Console.WriteLine($"  {i + 1}. Camera #{cameraIndices[i]} (index {cameraIndices[i]})");
            }

            Console.WriteLine();
            Console.Write($"Select camera (1-{cameraIndices.Length}): ");
            if (!int.TryParse(Console.ReadLine(), out int selection) || selection < 1 || selection > cameraIndices.Length)
            {
                Console.WriteLine("Invalid selection.");
                return;
            }

            var selectedIndex = cameraIndices[selection - 1];
            Console.WriteLine($"Selected camera #{selectedIndex}");

            ICamera camera = CameraFactory.CreateLocal(selectedIndex, $"Local Camera #{selectedIndex}");

            // Register device in DeviceManager
            deviceManager.RegisterDevice(camera);

            try
            {
                Console.WriteLine("Connecting to camera...");
                await camera.ConnectAsync();
                await camera.InitializeAsync();

                var info = await camera.GetDeviceInfoAsync();
                Console.WriteLine($"✓ Connected to {info.DeviceName}");
                Console.WriteLine($"  Manufacturer: {info.Manufacturer}");
                Console.WriteLine($"  Model: {info.Model}");

                var resolutions = await camera.GetSupportedResolutionsAsync();
                Console.WriteLine($"  Supported resolutions: {string.Join(", ", resolutions.Select(r => r.ToString()))}");

                Console.WriteLine();
                Console.WriteLine("Capturing frame...");

                var frame = await camera.CaptureFrameAsync();

                var filename = $"camera_{selectedIndex}_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
                var filepath = Path.Combine(AppContext.BaseDirectory, filename);
                await camera.SaveFrameAsync(frame, filepath);

                Console.WriteLine($"✓ Frame captured and saved:");
                Console.WriteLine($"  File: {filename}");
                Console.WriteLine($"  Path: {filepath}");
                Console.WriteLine($"  Resolution: {frame.Resolution}");
                Console.WriteLine($"  Format: {frame.Format}");
                Console.WriteLine($"  Size: {frame.Data.Length / 1024} KB");
                Console.WriteLine($"  Time: {frame.Timestamp:dd.MM.yyyy HH:mm:ss}");
                Console.WriteLine($"  Frame number: {frame.FrameNumber}");
                Console.WriteLine($"✓ Device registered in DeviceManager with ID: {camera.DeviceId}");

                Console.WriteLine();
                Console.Write("Start video stream for 5 seconds? (y/n): ");
                if (Console.ReadLine()?.ToLower() == "y")
                {
                    Console.WriteLine("Starting stream...");
                    int frameCount = 0;
                    camera.FrameCaptured += (s, e) =>
                    {
                        frameCount++;
                        Console.WriteLine($"  Received frame #{e.Frame.FrameNumber}, size: {e.Frame.Data.Length / 1024} KB");
                    };

                    await camera.StartStreamingAsync();
                    await Task.Delay(5000);
                    await camera.StopStreamingAsync();

                    Console.WriteLine($"✓ Stream stopped. Captured frames: {frameCount}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Camera error: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
            }
        }
    }
}

