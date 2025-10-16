namespace Prometheus.Devices.Core.Interfaces
{
    /// <summary>
    /// Интерфейс для работы со сканерами
    /// </summary>
    public interface IScanner : IDevice
    {
        /// <summary>
        /// Текущие настройки сканера
        /// </summary>
        ScannerSettings Settings { get; set; }

        /// <summary>
        /// Сканировать изображение
        /// </summary>
        Task<ScannedImage> ScanAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Получить поддерживаемые разрешения
        /// </summary>
        Task<int[]> GetSupportedResolutionsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Сохранить отсканированное изображение
        /// </summary>
        Task<bool> SaveImageAsync(ScannedImage image, string filePath, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Настройки сканера
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
    /// Режим цвета сканирования
    /// </summary>
    public enum ScanColorMode
    {
        BlackAndWhite,
        Grayscale,
        Color
    }

    /// <summary>
    /// Формат сканирования
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
    /// Отсканированное изображение
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

