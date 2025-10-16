using Prometheus.Devices.Core.Interfaces;

namespace Prometheus.Devices.Core.Platform
{
    /// <summary>
    /// Платформо-специфичный интерфейс для сканирования
    /// </summary>
    public interface IPlatformScanner
    {
        /// <summary>
        /// Получить список доступных сканеров в системе
        /// </summary>
        Task<string[]> GetAvailableScannersAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Сканировать изображение
        /// </summary>
        Task<ScannedImage> ScanAsync(string scannerName, ScannerSettings settings, CancellationToken cancellationToken = default);

        /// <summary>
        /// Проверить доступность сканера
        /// </summary>
        Task<bool> IsScannerAvailableAsync(string scannerName, CancellationToken cancellationToken = default);
    }
}

