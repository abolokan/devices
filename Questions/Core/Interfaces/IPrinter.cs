using System;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceWrappers.Core.Interfaces
{
    /// <summary>
    /// Интерфейс для работы с принтерами
    /// </summary>
    public interface IPrinter : IDevice
    {
        /// <summary>
        /// Текущие настройки принтера
        /// </summary>
        PrinterSettings Settings { get; set; }

        /// <summary>
        /// Статус принтера
        /// </summary>
        PrinterStatus PrinterStatus { get; }

        /// <summary>
        /// Событие изменения статуса печати
        /// </summary>
        event EventHandler<PrintJobStatusChangedEventArgs> PrintJobStatusChanged;

        /// <summary>
        /// Напечатать документ
        /// </summary>
        Task<PrintJob> PrintAsync(byte[] data, PrintOptions options = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Напечатать текст
        /// </summary>
        Task<PrintJob> PrintTextAsync(string text, PrintOptions options = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Напечатать файл
        /// </summary>
        Task<PrintJob> PrintFileAsync(string filePath, PrintOptions options = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Отменить задание печати
        /// </summary>
        Task<bool> CancelPrintJobAsync(string jobId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Получить статус задания печати
        /// </summary>
        Task<PrintJobStatus> GetPrintJobStatusAsync(string jobId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Получить уровень расходных материалов
        /// </summary>
        Task<ConsumablesLevel> GetConsumablesLevelAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Настройки принтера
    /// </summary>
    public class PrinterSettings
    {
        public PaperSize PaperSize { get; set; } = PaperSize.A4;
        public int DPI { get; set; } = 300;
        public bool ColorPrint { get; set; } = false;
        public PrintQuality Quality { get; set; } = PrintQuality.Normal;
    }

    /// <summary>
    /// Опции печати
    /// </summary>
    public class PrintOptions
    {
        public int Copies { get; set; } = 1;
        public bool Duplex { get; set; } = false;
        public PaperSize PaperSize { get; set; } = PaperSize.A4;
        public PrintQuality Quality { get; set; } = PrintQuality.Normal;
        public bool ColorPrint { get; set; } = false;
    }

    /// <summary>
    /// Размер бумаги
    /// </summary>
    public enum PaperSize
    {
        A4,
        A3,
        Letter,
        Legal,
        Custom
    }

    /// <summary>
    /// Качество печати
    /// </summary>
    public enum PrintQuality
    {
        Draft,
        Normal,
        High,
        Best
    }

    /// <summary>
    /// Статус принтера
    /// </summary>
    public enum PrinterStatus
    {
        Idle,
        Printing,
        PaperJam,
        OutOfPaper,
        OutOfToner,
        Error
    }

    /// <summary>
    /// Задание печати
    /// </summary>
    public class PrintJob
    {
        public string JobId { get; set; }
        public string DocumentName { get; set; }
        public int TotalPages { get; set; }
        public int PrintedPages { get; set; }
        public PrintJobStatus Status { get; set; }
        public DateTime SubmittedAt { get; set; }
    }

    /// <summary>
    /// Статус задания печати
    /// </summary>
    public enum PrintJobStatus
    {
        Queued,
        Printing,
        Completed,
        Cancelled,
        Error
    }

    /// <summary>
    /// Уровень расходных материалов
    /// </summary>
    public class ConsumablesLevel
    {
        public int TonerLevel { get; set; } // 0-100%
        public int PaperLevel { get; set; } // 0-100%
        public int DrumLevel { get; set; } // 0-100%
    }

    /// <summary>
    /// Аргументы события изменения статуса печати
    /// </summary>
    public class PrintJobStatusChangedEventArgs : EventArgs
    {
        public string JobId { get; set; }
        public PrintJobStatus OldStatus { get; set; }
        public PrintJobStatus NewStatus { get; set; }
        public int Progress { get; set; }
    }
}

