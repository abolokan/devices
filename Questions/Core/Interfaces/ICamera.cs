using System;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceWrappers.Core.Interfaces
{
    /// <summary>
    /// Интерфейс для работы с камерами
    /// </summary>
    public interface ICamera : IDevice
    {
        /// <summary>
        /// Текущие настройки камеры
        /// </summary>
        CameraSettings Settings { get; set; }

        /// <summary>
        /// Событие получения нового кадра
        /// </summary>
        event EventHandler<FrameCapturedEventArgs> FrameCaptured;

        /// <summary>
        /// Захватить одиночный кадр
        /// </summary>
        Task<CameraFrame> CaptureFrameAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Начать непрерывную съемку
        /// </summary>
        Task<bool> StartStreamingAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Остановить непрерывную съемку
        /// </summary>
        Task<bool> StopStreamingAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Получить поддерживаемые разрешения
        /// </summary>
        Task<Resolution[]> GetSupportedResolutionsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Сохранить кадр в файл
        /// </summary>
        Task<bool> SaveFrameAsync(CameraFrame frame, string filePath, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Настройки камеры
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
    /// Разрешение изображения
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
    /// Формат изображения
    /// </summary>
    public enum ImageFormat
    {
        JPEG,
        PNG,
        BMP,
        RAW
    }

    /// <summary>
    /// Кадр с камеры
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
    /// Аргументы события захвата кадра
    /// </summary>
    public class FrameCapturedEventArgs : EventArgs
    {
        public CameraFrame Frame { get; set; }
    }
}

