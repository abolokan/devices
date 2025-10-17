using System.Text;
using Prometheus.Devices.Abstractions.Interfaces;
using Prometheus.Devices.Core.Devices;

namespace Prometheus.Devices.Printers
{
    /// <summary>
    /// Generic printer implementation using PJL protocol
    /// Note: Primarily for Windows. For Linux, use DriverPrinter with ESC/POS or OfficePrinter with CUPS.
    /// PJL commands may not work through Linux CUPS without RAW queue configuration.
    /// </summary>
    public class GenericPrinter : BaseDevice, IPrinter
    {
        private PrinterSettings _settings;
        private PrinterStatus _printerStatus = PrinterStatus.Idle;
        private readonly Dictionary<string, PrintJob> _printJobs = new Dictionary<string, PrintJob>();
        private readonly object _jobsLock = new object();

        public override DeviceType DeviceType => DeviceType.Printer;

        public PrinterSettings Settings
        {
            get => _settings;
            set
            {
                _settings = value ?? throw new ArgumentNullException(nameof(value));
                OnSettingsChanged();
            }
        }

        public PrinterStatus PrinterStatus
        {
            get => _printerStatus;
            private set
            {
                if (_printerStatus != value)
                {
                    _printerStatus = value;
                }
            }
        }

        public event EventHandler<PrintJobStatusChangedEventArgs> PrintJobStatusChanged;

        public GenericPrinter(string deviceId, string deviceName, IConnection connection)
            : base(deviceId, deviceName, connection)
        {
            _settings = new PrinterSettings();
        }

        protected override async Task OnInitializeAsync(CancellationToken cancellationToken)
        {
            // Initialize printer
            byte[] initCommand = Encoding.ASCII.GetBytes("@PJL INITIALIZE\r\n");
            await Connection.SendAsync(initCommand, cancellationToken);

            await Task.Delay(500, cancellationToken);

            // Check status
            await UpdatePrinterStatusAsync(cancellationToken);
        }

        protected override async Task<DeviceInfo> OnGetDeviceInfoAsync(CancellationToken cancellationToken)
        {
            // Request device information
            byte[] command = Encoding.ASCII.GetBytes("@PJL INFO ID\r\n");
            await Connection.SendAsync(command, cancellationToken);

            byte[] response = await Connection.ReceiveAsync(1024, cancellationToken);
            string responseStr = Encoding.ASCII.GetString(response);

            return new DeviceInfo
            {
                DeviceId = DeviceId,
                DeviceName = DeviceName,
                DeviceType = DeviceType.Printer,
                Manufacturer = "Generic",
                Model = "Printer Model",
                FirmwareVersion = "1.0.0",
                SerialNumber = DeviceId
            };
        }

        protected override async Task OnResetAsync(CancellationToken cancellationToken)
        {
            // Cancel all jobs
            lock (_jobsLock)
            {
                foreach (var job in _printJobs.Values)
                {
                    if (job.Status == PrintJobStatus.Queued || job.Status == PrintJobStatus.Printing)
                    {
                        job.Status = PrintJobStatus.Cancelled;
                    }
                }
                _printJobs.Clear();
            }

            // Reset printer
            byte[] resetCommand = Encoding.ASCII.GetBytes("@PJL RESET\r\n");
            await Connection.SendAsync(resetCommand, cancellationToken);
            await Task.Delay(2000, cancellationToken);
        }

        public async Task<PrintJob> PrintAsync(byte[] data, PrintOptions options = null, CancellationToken cancellationToken = default)
        {
            ThrowIfNotReady();

            if (data == null || data.Length == 0)
                throw new ArgumentException("Print data cannot be empty", nameof(data));

            options ??= new PrintOptions();

            return await RetryPolicy.ExecuteAsync(async () =>
            {
                try
                {
                    SetStatus(DeviceStatus.Busy, "Preparing to print...");

                    var printJob = new PrintJob
                    {
                        JobId = Guid.NewGuid().ToString(),
                        DocumentName = $"Document_{DateTime.Now:yyyyMMdd_HHmmss}",
                        TotalPages = EstimatePages(data, options),
                        PrintedPages = 0,
                        Status = PrintJobStatus.Queued,
                        SubmittedAt = DateTime.Now
                    };

                    lock (_jobsLock)
                    {
                        _printJobs[printJob.JobId] = printJob;
                    }

                    _ = Task.Run(async () => await ExecutePrintJobAsync(printJob, data, options, cancellationToken), cancellationToken);

                    return printJob;
                }
                catch (Exception ex)
                {
                    SetStatus(DeviceStatus.Error, $"Print error: {ex.Message}");
                    throw;
                }
            }, cancellationToken);
        }

        public async Task<PrintJob> PrintTextAsync(string text, PrintOptions options = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(text))
                throw new ArgumentException("Text cannot be empty", nameof(text));

            byte[] data = Encoding.UTF8.GetBytes(text);
            return await PrintAsync(data, options, cancellationToken);
        }

        public async Task<PrintJob> PrintFileAsync(string filePath, PrintOptions options = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be empty", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException("File not found", filePath);

            byte[] data = await File.ReadAllBytesAsync(filePath, cancellationToken);
            return await PrintAsync(data, options, cancellationToken);
        }

        public async Task<bool> CancelPrintJobAsync(string jobId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(jobId))
                throw new ArgumentException("Job ID cannot be empty", nameof(jobId));

            PrintJob job;
            lock (_jobsLock)
            {
                if (!_printJobs.TryGetValue(jobId, out job))
                    return false;

                if (job.Status == PrintJobStatus.Completed || job.Status == PrintJobStatus.Cancelled)
                    return false;

                job.Status = PrintJobStatus.Cancelled;
            }

            // Send cancel command
            byte[] cancelCommand = Encoding.ASCII.GetBytes($"@PJL CANCEL {jobId}\r\n");
            await Connection.SendAsync(cancelCommand, cancellationToken);

            OnPrintJobStatusChanged(new PrintJobStatusChangedEventArgs
            {
                JobId = jobId,
                OldStatus = PrintJobStatus.Printing,
                NewStatus = PrintJobStatus.Cancelled,
                Progress = job.PrintedPages * 100 / job.TotalPages
            });

            return true;
        }

        public Task<PrintJobStatus> GetPrintJobStatusAsync(string jobId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(jobId))
                throw new ArgumentException("Job ID cannot be empty", nameof(jobId));

            lock (_jobsLock)
            {
                if (_printJobs.TryGetValue(jobId, out var job))
                {
                    return Task.FromResult(job.Status);
                }
            }

            return Task.FromResult(PrintJobStatus.Error);
        }

        public async Task<ConsumablesLevel> GetConsumablesLevelAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfNotReady();

            return await RetryPolicy.ExecuteAsync(async () =>
            {
                byte[] command = Encoding.ASCII.GetBytes("@PJL INFO SUPPLIES\r\n");
                await Connection.SendAsync(command, cancellationToken);

                byte[] response = await Connection.ReceiveAsync(1024, cancellationToken);
                string responseStr = Encoding.ASCII.GetString(response);

                return new ConsumablesLevel
                {
                    TonerLevel = 85,
                    PaperLevel = 70,
                    DrumLevel = 90
                };
            }, cancellationToken);
        }

        private async Task ExecutePrintJobAsync(PrintJob job, byte[] data, PrintOptions options, CancellationToken cancellationToken)
        {
            try
            {
                PrinterStatus = PrinterStatus.Printing;
                job.Status = PrintJobStatus.Printing;

                OnPrintJobStatusChanged(new PrintJobStatusChangedEventArgs
                {
                    JobId = job.JobId,
                    OldStatus = PrintJobStatus.Queued,
                    NewStatus = PrintJobStatus.Printing,
                    Progress = 0
                });

                // Build print commands
                StringBuilder printCommands = new StringBuilder();
                printCommands.AppendLine($"@PJL JOB NAME=\"{job.DocumentName}\"");
                printCommands.AppendLine($"@PJL SET COPIES={options.Copies}");
                printCommands.AppendLine($"@PJL SET DUPLEX={(options.Duplex ? "ON" : "OFF")}");
                printCommands.AppendLine($"@PJL SET PAPER={options.PaperSize}");
                printCommands.AppendLine($"@PJL SET QUALITY={options.Quality}");

                byte[] header = Encoding.ASCII.GetBytes(printCommands.ToString());
                await Connection.SendAsync(header, cancellationToken);

                // Send data in chunks with progress updates
                int chunkSize = 8192;
                int totalSent = 0;

                for (int i = 0; i < data.Length; i += chunkSize)
                {
                    if (job.Status == PrintJobStatus.Cancelled)
                        break;

                    int size = Math.Min(chunkSize, data.Length - i);
                    byte[] chunk = new byte[size];
                    Array.Copy(data, i, chunk, 0, size);

                    await Connection.SendAsync(chunk, cancellationToken);
                    totalSent += size;

                    // Update progress
                    int printedPages = (int)((double)totalSent / data.Length * job.TotalPages);
                    if (printedPages > job.PrintedPages)
                    {
                        job.PrintedPages = printedPages;
                        OnPrintJobStatusChanged(new PrintJobStatusChangedEventArgs
                        {
                            JobId = job.JobId,
                            OldStatus = PrintJobStatus.Printing,
                            NewStatus = PrintJobStatus.Printing,
                            Progress = printedPages * 100 / job.TotalPages
                        });
                    }

                    await Task.Delay(50, cancellationToken); // Simulate print time
                }

                // Complete job
                byte[] footer = Encoding.ASCII.GetBytes("@PJL EOJ\r\n");
                await Connection.SendAsync(footer, cancellationToken);

                if (job.Status != PrintJobStatus.Cancelled)
                {
                    job.Status = PrintJobStatus.Completed;
                    job.PrintedPages = job.TotalPages;

                    OnPrintJobStatusChanged(new PrintJobStatusChangedEventArgs
                    {
                        JobId = job.JobId,
                        OldStatus = PrintJobStatus.Printing,
                        NewStatus = PrintJobStatus.Completed,
                        Progress = 100
                    });
                }

                PrinterStatus = PrinterStatus.Idle;
                SetStatus(DeviceStatus.Ready, "Print completed");
            }
            catch (Exception ex)
            {
                job.Status = PrintJobStatus.Error;
                PrinterStatus = PrinterStatus.Error;
                SetStatus(DeviceStatus.Error, $"Print error: {ex.Message}");

                OnPrintJobStatusChanged(new PrintJobStatusChangedEventArgs
                {
                    JobId = job.JobId,
                    OldStatus = PrintJobStatus.Printing,
                    NewStatus = PrintJobStatus.Error,
                    Progress = job.PrintedPages * 100 / job.TotalPages
                });
            }
        }

        private async Task UpdatePrinterStatusAsync(CancellationToken cancellationToken)
        {
            byte[] statusCommand = Encoding.ASCII.GetBytes("@PJL INFO STATUS\r\n");
            await Connection.SendAsync(statusCommand, cancellationToken);

            byte[] response = await Connection.ReceiveAsync(512, cancellationToken);
            string statusStr = Encoding.ASCII.GetString(response);

            // Parse status (simplified)
            PrinterStatus = PrinterStatus.Idle;
        }

        private int EstimatePages(byte[] data, PrintOptions options)
        {
            // Simplified page count estimation
            // In reality, need to analyze document format
            int estimatedPages = Math.Max(1, data.Length / 4096);
            return estimatedPages;
        }

        private void OnSettingsChanged()
        {
        }

        protected virtual void OnPrintJobStatusChanged(PrintJobStatusChangedEventArgs e)
        {
            PrintJobStatusChanged?.Invoke(this, e);
        }

        public override void Dispose()
        {
            lock (_jobsLock)
            {
                _printJobs.Clear();
            }

            base.Dispose();
        }
    }
}

