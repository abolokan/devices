using Prometheus.Devices.Core.Connections;
using Prometheus.Devices.Core.Devices;
using Prometheus.Devices.Core.Interfaces;
using Prometheus.Devices.Core.Platform;

namespace Prometheus.Devices.Printers
{
    /// <summary>
    /// Office printer (laser/inkjet) via platform-specific API
    /// Cross-platform: Uses PrintDocument on Windows, CUPS/lp on Linux
    /// Requires: Printer installed in OS with appropriate driver
    /// </summary>
    public class OfficePrinter : BaseDevice, IPrinter
    {
        private readonly IPlatformPrinter _platformPrinter;
        private readonly string _systemPrinterName;
        private PrinterSettings _settings;
        private PrinterStatus _printerStatus = PrinterStatus.Idle;

        public override DeviceType DeviceType => DeviceType.Printer;

        public PrinterSettings Settings
        {
            get => _settings;
            set => _settings = value ?? throw new ArgumentNullException(nameof(value));
        }

        public PrinterStatus PrinterStatus => _printerStatus;

        public event EventHandler<PrintJobStatusChangedEventArgs> PrintJobStatusChanged;

        public OfficePrinter(string deviceId, string deviceName, string systemPrinterName, IPlatformPrinter platformPrinter)
            : base(deviceId, deviceName, new EmbeddedConnection())
        {
            _systemPrinterName = systemPrinterName ?? throw new ArgumentNullException(nameof(systemPrinterName));
            _platformPrinter = platformPrinter ?? throw new ArgumentNullException(nameof(platformPrinter));
            _settings = new PrinterSettings();
        }

        protected override async Task OnInitializeAsync(CancellationToken cancellationToken)
        {
            var isAvailable = await _platformPrinter.IsPrinterAvailableAsync(_systemPrinterName, cancellationToken);
            if (!isAvailable)
                throw new InvalidOperationException($"Printer '{_systemPrinterName}' not available in system");
        }

        protected override async Task<DeviceInfo> OnGetDeviceInfoAsync(CancellationToken cancellationToken)
        {
            return new DeviceInfo
            {
                DeviceId = DeviceId,
                DeviceName = DeviceName,
                DeviceType = DeviceType.Printer,
                Manufacturer = "Office Printer",
                Model = _systemPrinterName,
                FirmwareVersion = "N/A",
                SerialNumber = DeviceId
            };
        }

        protected override Task OnResetAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public async Task<PrintJob> PrintAsync(byte[] data, PrintOptions options = null, CancellationToken cancellationToken = default)
        {
            ThrowIfNotReady();

            return await RetryPolicy.ExecuteAsync(async () =>
            {
                var tempFile = Path.GetTempFileName();
                await File.WriteAllBytesAsync(tempFile, data, cancellationToken);

                try
                {
                    _printerStatus = PrinterStatus.Printing;
                    var jobId = await _platformPrinter.PrintFileAsync(_systemPrinterName, tempFile, cancellationToken);

                    var printJob = new PrintJob
                    {
                        JobId = jobId,
                        DocumentName = "OfficePrint",
                        TotalPages = 1,
                        PrintedPages = 1,
                        Status = PrintJobStatus.Completed,
                        SubmittedAt = DateTime.Now
                    };

                    _printerStatus = PrinterStatus.Idle;
                    OnPrintJobStatusChanged(jobId, PrintJobStatus.Queued, PrintJobStatus.Completed, 100);

                    return printJob;
                }
                finally
                {
                    if (System.IO.File.Exists(tempFile))
                        System.IO.File.Delete(tempFile);
                }
            }, cancellationToken);
        }

        public async Task<PrintJob> PrintTextAsync(string text, PrintOptions options = null, CancellationToken cancellationToken = default)
        {
            ThrowIfNotReady();

            return await RetryPolicy.ExecuteAsync(async () =>
            {
                _printerStatus = PrinterStatus.Printing;
                var jobId = await _platformPrinter.PrintTextAsync(_systemPrinterName, text, cancellationToken);

                var printJob = new PrintJob
                {
                    JobId = jobId,
                    DocumentName = "TextPrint",
                    TotalPages = 1,
                    PrintedPages = 1,
                    Status = PrintJobStatus.Completed,
                    SubmittedAt = DateTime.Now
                };

                _printerStatus = PrinterStatus.Idle;
                OnPrintJobStatusChanged(jobId, PrintJobStatus.Queued, PrintJobStatus.Completed, 100);

                return printJob;
            }, cancellationToken);
        }

        public async Task<PrintJob> PrintFileAsync(string filePath, PrintOptions options = null, CancellationToken cancellationToken = default)
        {
            ThrowIfNotReady();

            return await RetryPolicy.ExecuteAsync(async () =>
            {
                _printerStatus = PrinterStatus.Printing;
                var jobId = await _platformPrinter.PrintFileAsync(_systemPrinterName, filePath, cancellationToken);

                var printJob = new PrintJob
                {
                    JobId = jobId,
                    DocumentName = System.IO.Path.GetFileName(filePath),
                    TotalPages = 1,
                    PrintedPages = 1,
                    Status = PrintJobStatus.Completed,
                    SubmittedAt = DateTime.Now
                };

                _printerStatus = PrinterStatus.Idle;
                OnPrintJobStatusChanged(jobId, PrintJobStatus.Queued, PrintJobStatus.Completed, 100);

                return printJob;
            }, cancellationToken);
        }

        public Task<bool> CancelPrintJobAsync(string jobId, CancellationToken cancellationToken = default)
        {
            // Platform-specific cancel not implemented
            return Task.FromResult(false);
        }

        public Task<PrintJobStatus> GetPrintJobStatusAsync(string jobId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(PrintJobStatus.Completed);
        }

        public Task<ConsumablesLevel> GetConsumablesLevelAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ConsumablesLevel { TonerLevel = -1, PaperLevel = -1, DrumLevel = -1 });
        }

        private void OnPrintJobStatusChanged(string jobId, PrintJobStatus oldStatus, PrintJobStatus newStatus, int progress)
        {
            PrintJobStatusChanged?.Invoke(this, new PrintJobStatusChangedEventArgs
            {
                JobId = jobId,
                OldStatus = oldStatus,
                NewStatus = newStatus,
                Progress = progress
            });
        }
    }
}

