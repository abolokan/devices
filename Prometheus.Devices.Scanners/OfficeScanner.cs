using Prometheus.Devices.Core.Connections;
using Prometheus.Devices.Core.Devices;
using Prometheus.Devices.Core.Interfaces;
using Prometheus.Devices.Core.Platform;

namespace DeviceWrappers.Devices.Scanner
{
    /// <summary>
    /// Офисный сканер через платформо-специфичные API
    /// </summary>
    public class OfficeScanner : BaseDevice, IScanner
    {
        private readonly IPlatformScanner _platformScanner;
        private readonly string _systemScannerName;
        private ScannerSettings _settings;

        public override DeviceType DeviceType => DeviceType.Scanner;

        public ScannerSettings Settings
        {
            get => _settings;
            set => _settings = value ?? throw new ArgumentNullException(nameof(value));
        }

        public OfficeScanner(string deviceId, string deviceName, string systemScannerName, IPlatformScanner platformScanner)
            : base(deviceId, deviceName, new NullConnection())
        {
            _systemScannerName = systemScannerName ?? throw new ArgumentNullException(nameof(systemScannerName));
            _platformScanner = platformScanner ?? throw new ArgumentNullException(nameof(platformScanner));
            _settings = new ScannerSettings();
        }

        protected override async Task OnInitializeAsync(CancellationToken cancellationToken)
        {
            var isAvailable = await _platformScanner.IsScannerAvailableAsync(_systemScannerName, cancellationToken);
            if (!isAvailable)
                throw new InvalidOperationException($"Scanner '{_systemScannerName}' not available in system");
        }

        protected override Task<DeviceInfo> OnGetDeviceInfoAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(new DeviceInfo
            {
                DeviceId = DeviceId,
                DeviceName = DeviceName,
                DeviceType = DeviceType.Scanner,
                Manufacturer = "Office Scanner",
                Model = _systemScannerName,
                FirmwareVersion = "N/A",
                SerialNumber = DeviceId
            });
        }

        protected override Task OnResetAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public async Task<ScannedImage> ScanAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfNotReady();
            SetStatus(DeviceStatus.Busy, "Scanning...");

            try
            {
                var image = await _platformScanner.ScanAsync(_systemScannerName, Settings, cancellationToken);
                SetStatus(DeviceStatus.Ready, "Scan completed");
                return image;
            }
            catch (Exception ex)
            {
                SetStatus(DeviceStatus.Error, $"Scan error: {ex.Message}");
                throw;
            }
        }

        public Task<int[]> GetSupportedResolutionsAsync(CancellationToken cancellationToken = default)
        {
            // Common resolutions for office scanners
            return Task.FromResult(new[] { 75, 150, 200, 300, 600, 1200 });
        }

        public async Task<bool> SaveImageAsync(ScannedImage image, string filePath, CancellationToken cancellationToken = default)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be empty", nameof(filePath));

            try
            {
                await File.WriteAllBytesAsync(filePath, image.Data, cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                throw new IOException($"Failed to save scanned image: {ex.Message}", ex);
            }
        }
    }
}

