using System.Text;
using Prometheus.Devices.Core.Devices;
using Prometheus.Devices.Core.Drivers;
using Prometheus.Devices.Core.Interfaces;
using Prometheus.Devices.Core.Profiles;
using PrinterBarcodeType = Prometheus.Devices.Core.Drivers.BarcodeType;

namespace DeviceWrappers.Devices.Printer
{   
    public class DriverPrinter : BaseDevice, IPrinter
    {
        private readonly IPrinterDriver _driver;
        private readonly PrinterProfile _profile;
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

        public DriverPrinter(string deviceId, string deviceName, IConnection connection, IPrinterDriver driver, PrinterProfile profile)
            : base(deviceId, deviceName, connection)
        {
            _driver = driver ?? throw new ArgumentNullException(nameof(driver));
            _profile = profile ?? new PrinterProfile();
            _settings = new PrinterSettings();
        }

        protected override async Task OnInitializeAsync(CancellationToken cancellationToken)
        {
            var init = _driver.BuildInitialize();
            if (init?.Length > 0)
                await Connection.SendAsync(init, cancellationToken);

            var cpId = _profile.EscPosCodepage ?? 0;
            var cp = _driver.BuildSetCodepage(cpId);
            if (cp?.Length > 0)
                await Connection.SendAsync(cp, cancellationToken);
        }

        protected override Task<DeviceInfo> OnGetDeviceInfoAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(new DeviceInfo
            {
                DeviceId = DeviceId,
                DeviceName = DeviceName,
                DeviceType = DeviceType.Printer,
                Manufacturer = _profile.Manufacturer,
                Model = _profile.Model,
                FirmwareVersion = _profile.Version,
                SerialNumber = DeviceId
            });
        }

        protected override Task OnResetAsync(CancellationToken cancellationToken)
        {
            // For most ESC/POS, reinitializing (ESC @) is enough
            return Task.CompletedTask;
        }

        public Task<PrintJob> PrintAsync(byte[] data, PrintOptions options = null, CancellationToken cancellationToken = default)
        {
            options ??= new PrintOptions();

            return Task.Run(async () =>
            {
                var job = new PrintJob
                {
                    JobId = Guid.NewGuid().ToString(),
                    DocumentName = "DriverPrint",
                    TotalPages = 1,
                    PrintedPages = 0,
                    Status = PrintJobStatus.Queued,
                    SubmittedAt = DateTime.Now
                };

                OnPrintJobStatusChanged(job.JobId, PrintJobStatus.Queued, PrintJobStatus.Printing, 0);

                var payload = _driver.BuildRaw(data);
                if (payload?.Length > 0)
                    await Connection.SendAsync(payload, cancellationToken);

                var feed = _driver.BuildFeedLines(Math.Max(1, _profile.DefaultFeedLines));
                if (feed?.Length > 0)
                    await Connection.SendAsync(feed, cancellationToken);

                if (_profile.SupportsCut)
                {
                    var cut = _driver.BuildCut(_profile.PartialCut);
                    if (cut?.Length > 0)
                        await Connection.SendAsync(cut, cancellationToken);
                }

                job.Status = PrintJobStatus.Completed;
                job.PrintedPages = 1;
                OnPrintJobStatusChanged(job.JobId, PrintJobStatus.Printing, PrintJobStatus.Completed, 100);
                return job;
            }, cancellationToken);
        }

        public Task<PrintJob> PrintTextAsync(string text, PrintOptions options = null, CancellationToken cancellationToken = default)
        {
            Encoding enc;
            try
            {
                enc = _profile.DefaultCodepage > 0 ? Encoding.GetEncoding(_profile.DefaultCodepage) : Encoding.ASCII;
            }
            catch
            {
                enc = Encoding.ASCII;
            }
            var data = _driver.BuildPrintText(text + "\r\n", enc);
            return PrintAsync(data, options, cancellationToken);
        }

        public Task<PrintJob> PrintFileAsync(string filePath, PrintOptions options = null, CancellationToken cancellationToken = default)
        {
            var bytes = System.IO.File.ReadAllBytes(filePath);
            return PrintAsync(bytes, options, cancellationToken);
        }

        public Task<bool> CancelPrintJobAsync(string jobId, CancellationToken cancellationToken = default)
        {
            // For RAW/ESC-POS, cancellation often not supported after sending
            return Task.FromResult(false);
        }

        public Task<PrintJobStatus> GetPrintJobStatusAsync(string jobId, CancellationToken cancellationToken = default)
        {
            // RAW mode doesn't provide status - return Completed as fact
            return Task.FromResult(PrintJobStatus.Completed);
        }

        public Task<ConsumablesLevel> GetConsumablesLevelAsync(CancellationToken cancellationToken = default)
        {
            // In RAW mode, usually unavailable; return unknown
            return Task.FromResult(new ConsumablesLevel { TonerLevel = -1, PaperLevel = -1, DrumLevel = -1 });
        }

        /// <summary>
        /// Print barcode (Linux/Windows compatible)
        /// </summary>
        public async Task<PrintJob> PrintBarcodeAsync(
            string data, 
            PrinterBarcodeType type = PrinterBarcodeType.Code128,
            int height = 100, 
            int width = 3,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNotReady();

            var barcodeCmd = _driver.BuildPrintBarcode(data, type, height, width);
            var feedCmd = _driver.BuildFeedLines(_profile.DefaultFeedLines);

            var commands = new List<byte>();
            commands.AddRange(barcodeCmd);
            commands.AddRange(feedCmd);

            if (_profile.SupportsCut)
            {
                var cutCmd = _driver.BuildCut(_profile.PartialCut);
                commands.AddRange(cutCmd);
            }

            await Connection.SendAsync(commands.ToArray(), cancellationToken);

            return new PrintJob
            {
                JobId = Guid.NewGuid().ToString(),
                DocumentName = $"Barcode_{type}_{data}",
                TotalPages = 1,
                PrintedPages = 1,
                Status = PrintJobStatus.Completed,
                SubmittedAt = DateTime.Now
            };
        }

        /// <summary>
        /// Print QR code (Linux/Windows compatible)
        /// </summary>
        public async Task<PrintJob> PrintQrCodeAsync(
            string data,
            int size = 6,
            QrErrorCorrection errorLevel = QrErrorCorrection.M,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNotReady();

            var qrCmd = _driver.BuildPrintQrCode(data, size, errorLevel);
            var feedCmd = _driver.BuildFeedLines(_profile.DefaultFeedLines);

            var commands = new List<byte>();
            commands.AddRange(qrCmd);
            commands.AddRange(feedCmd);

            if (_profile.SupportsCut)
            {
                var cutCmd = _driver.BuildCut(_profile.PartialCut);
                commands.AddRange(cutCmd);
            }

            await Connection.SendAsync(commands.ToArray(), cancellationToken);

            return new PrintJob
            {
                JobId = Guid.NewGuid().ToString(),
                DocumentName = $"QRCode_{data.Substring(0, Math.Min(20, data.Length))}",
                TotalPages = 1,
                PrintedPages = 1,
                Status = PrintJobStatus.Completed,
                SubmittedAt = DateTime.Now
            };
        }

        /// <summary>
        /// Print label with barcode and text (common packaging scenario)
        /// Linux/Windows compatible
        /// </summary>
        public async Task<PrintJob> PrintLabelAsync(
            string title,
            string barcodeData,
            PrinterBarcodeType barcodeType = PrinterBarcodeType.Code128,
            string[]? additionalLines = null,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNotReady();

            var encoding = Encoding.GetEncoding(_profile.DefaultCodepage);
            var commands = new List<byte>();

            // Print title
            if (!string.IsNullOrEmpty(title))
            {
                var titleBytes = _driver.BuildPrintText(title, encoding);
                commands.AddRange(titleBytes);
                commands.AddRange(_driver.BuildFeedLines(1));
            }

            // Print barcode
            var barcodeCmd = _driver.BuildPrintBarcode(barcodeData, barcodeType);
            commands.AddRange(barcodeCmd);
            commands.AddRange(_driver.BuildFeedLines(1));

            // Print additional lines
            if (additionalLines != null)
            {
                foreach (var line in additionalLines)
                {
                    var lineBytes = _driver.BuildPrintText(line, encoding);
                    commands.AddRange(lineBytes);
                    commands.AddRange(_driver.BuildFeedLines(1));
                }
            }

            // Feed and cut
            commands.AddRange(_driver.BuildFeedLines(_profile.DefaultFeedLines));
            if (_profile.SupportsCut)
            {
                var cutCmd = _driver.BuildCut(_profile.PartialCut);
                commands.AddRange(cutCmd);
            }

            await Connection.SendAsync(commands.ToArray(), cancellationToken);

            return new PrintJob
            {
                JobId = Guid.NewGuid().ToString(),
                DocumentName = $"Label_{barcodeData}",
                TotalPages = 1,
                PrintedPages = 1,
                Status = PrintJobStatus.Completed,
                SubmittedAt = DateTime.Now
            };
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