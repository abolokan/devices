namespace Prometheus.Devices.Core.Interfaces
{
    /// <summary>
    /// Interface for working with printers
    /// </summary>
    public interface IPrinter : IDevice
    {
        /// <summary>
        /// Current printer settings
        /// </summary>
        PrinterSettings Settings { get; set; }

        /// <summary>
        /// Printer status
        /// </summary>
        PrinterStatus PrinterStatus { get; }

        /// <summary>
        /// Print job status changed event
        /// </summary>
        event EventHandler<PrintJobStatusChangedEventArgs> PrintJobStatusChanged;

        /// <summary>
        /// Print document
        /// </summary>
        Task<PrintJob> PrintAsync(byte[] data, PrintOptions options = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Print text
        /// </summary>
        Task<PrintJob> PrintTextAsync(string text, PrintOptions options = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Print file
        /// </summary>
        Task<PrintJob> PrintFileAsync(string filePath, PrintOptions options = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancel print job
        /// </summary>
        Task<bool> CancelPrintJobAsync(string jobId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get print job status
        /// </summary>
        Task<PrintJobStatus> GetPrintJobStatusAsync(string jobId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get consumables level
        /// </summary>
        Task<ConsumablesLevel> GetConsumablesLevelAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Printer settings
    /// </summary>
    public class PrinterSettings
    {
        public PaperSize PaperSize { get; set; } = PaperSize.A4;
        public int DPI { get; set; } = 300;
        public bool ColorPrint { get; set; } = false;
        public PrintQuality Quality { get; set; } = PrintQuality.Normal;
    }

    /// <summary>
    /// Print options
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
    /// Paper size
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
    /// Print quality
    /// </summary>
    public enum PrintQuality
    {
        Draft,
        Normal,
        High,
        Best
    }

    /// <summary>
    /// Printer status
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
    /// Print job
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
    /// Print job status
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
    /// Consumables level
    /// </summary>
    public class ConsumablesLevel
    {
        public int TonerLevel { get; set; } // 0-100%
        public int PaperLevel { get; set; } // 0-100%
        public int DrumLevel { get; set; } // 0-100%
    }

    /// <summary>
    /// Print job status changed event arguments
    /// </summary>
    public class PrintJobStatusChangedEventArgs : EventArgs
    {
        public string JobId { get; set; }
        public PrintJobStatus OldStatus { get; set; }
        public PrintJobStatus NewStatus { get; set; }
        public int Progress { get; set; }
    }
}
