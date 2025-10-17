namespace Prometheus.Devices.Abstractions.Interfaces
{
    /// <summary>
    /// Interface for working with scanners
    /// </summary>
    public interface IScanner : IDevice
    {
        /// <summary>
        /// Current scanner settings
        /// </summary>
        ScannerSettings Settings { get; set; }

        /// <summary>
        /// Scan image
        /// </summary>
        Task<ScannedImage> ScanAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Get supported resolutions
        /// </summary>
        Task<int[]> GetSupportedResolutionsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Save scanned image
        /// </summary>
        Task<bool> SaveImageAsync(ScannedImage image, string filePath, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Scanner settings
    /// </summary>
    public class ScannerSettings
    {
        public int Resolution { get; set; } = 300; // DPI
        public ScanColorMode ColorMode { get; set; } = ScanColorMode.Color;
        public ScanFormat Format { get; set; } = ScanFormat.JPEG;
        public int Brightness { get; set; } = 0; // -127 to 127
        public int Contrast { get; set; } = 0; // -127 to 127
    }

    /// <summary>
    /// Scan color mode
    /// </summary>
    public enum ScanColorMode
    {
        BlackAndWhite,
        Grayscale,
        Color
    }

    /// <summary>
    /// Scan format
    /// </summary>
    public enum ScanFormat
    {
        JPEG,
        PNG,
        BMP,
        TIFF,
        PDF
    }

    /// <summary>
    /// Scanned image
    /// </summary>
    public class ScannedImage
    {
        public byte[] Data { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int Resolution { get; set; }
        public ScanColorMode ColorMode { get; set; }
        public ScanFormat Format { get; set; }
        public DateTime Timestamp { get; set; }
    }
}

