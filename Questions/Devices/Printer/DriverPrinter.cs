using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DeviceWrappers.Core.Devices;
using DeviceWrappers.Core.Drivers;
using DeviceWrappers.Core.Interfaces;
using DeviceWrappers.Core.Profiles;

namespace DeviceWrappers.Devices.Printer
{
    /// <summary>
    /// Принтер, работающий через драйвер команд (ESC/POS, Bixolon и т.п.)
    /// </summary>
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

            var cpId = _profile.EscPosCodepage ?? 0; // если не задано — оставляем по умолчанию
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
            // Для большинства ESC/POS достаточно переинициализировать (ESC @)
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
            // Для RAW/ESC-POS отмена часто не поддерживается после отправки
            return Task.FromResult(false);
        }

        public Task<PrintJobStatus> GetPrintJobStatusAsync(string jobId, CancellationToken cancellationToken = default)
        {
            // RAW-режим не даёт статуса — вернём Completed как по факту
            return Task.FromResult(PrintJobStatus.Completed);
        }

        public Task<ConsumablesLevel> GetConsumablesLevelAsync(CancellationToken cancellationToken = default)
        {
            // В RAW-режиме чаще всего недоступно; возвращаем неизвестно
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


