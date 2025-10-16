namespace Prometheus.Devices.Core.Interfaces
{
    /// <summary>
    /// Interface for working with cameras
    /// </summary>
    public interface ICamera : IDevice
    {
        /// <summary>
        /// Current camera settings
        /// </summary>
        CameraSettings Settings { get; set; }

        /// <summary>
        /// Frame captured event
        /// </summary>
        event EventHandler<FrameCapturedEventArgs> FrameCaptured;

        /// <summary>
        /// Capture single frame
        /// </summary>
        Task<CameraFrame> CaptureFrameAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Start continuous capture
        /// </summary>
        Task<bool> StartStreamingAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Stop continuous capture
        /// </summary>
        Task<bool> StopStreamingAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Get supported resolutions
        /// </summary>
        Task<Resolution[]> GetSupportedResolutionsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Save frame to file
        /// </summary>
        Task<bool> SaveFrameAsync(CameraFrame frame, string filePath, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Camera settings
    /// </summary>
    public class CameraSettings
    {
        public Resolution Resolution { get; set; } = new Resolution(1920, 1080);
        public int FrameRate { get; set; } = 30;
        public int Brightness { get; set; } = 50;
        public int Contrast { get; set; } = 50;
        public int Saturation { get; set; } = 50;
        public ImageFormat Format { get; set; } = ImageFormat.JPEG;
        public bool AutoExposure { get; set; } = true;
        public bool AutoWhiteBalance { get; set; } = true;
    }

    /// <summary>
    /// Image resolution
    /// </summary>
    public class Resolution
    {
        public int Width { get; set; }
        public int Height { get; set; }

        public Resolution(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public override string ToString() => $"{Width}x{Height}";
    }

    /// <summary>
    /// Image format
    /// </summary>
    public enum ImageFormat
    {
        JPEG,
        PNG,
        BMP,
        RAW
    }

    /// <summary>
    /// Camera frame
    /// </summary>
    public class CameraFrame
    {
        public byte[] Data { get; set; }
        public Resolution Resolution { get; set; }
        public ImageFormat Format { get; set; }
        public DateTime Timestamp { get; set; }
        public long FrameNumber { get; set; }
    }

    /// <summary>
    /// Frame captured event arguments
    /// </summary>
    public class FrameCapturedEventArgs : EventArgs
    {
        public CameraFrame Frame { get; set; }
    }
}
