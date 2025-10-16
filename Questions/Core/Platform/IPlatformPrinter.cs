using System.Threading;
using System.Threading.Tasks;

namespace DeviceWrappers.Core.Platform
{
    /// <summary>
    /// Платформо-специфичный интерфейс для печати
    /// </summary>
    public interface IPlatformPrinter
    {
        /// <summary>
        /// Получить список доступных принтеров в системе
        /// </summary>
        Task<string[]> GetAvailablePrintersAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Печать текста
        /// </summary>
        Task<string> PrintTextAsync(string printerName, string text, CancellationToken cancellationToken = default);

        /// <summary>
        /// Печать файла (PDF, TXT, изображение)
        /// </summary>
        Task<string> PrintFileAsync(string printerName, string filePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Получить статус принтера
        /// </summary>
        Task<bool> IsPrinterAvailableAsync(string printerName, CancellationToken cancellationToken = default);
    }
}

