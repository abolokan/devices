using OpenCvSharp;
using Prometheus.Devices.Core.Connections;
using Prometheus.Devices.Core.Devices;
using Prometheus.Devices.Core.Interfaces;

namespace DeviceWrappers.Devices.Camera
{
    /// <summary>
    /// Local (built-in/USB) camera via OpenCvSharp VideoCapture
    /// </summary>
    public class LocalCamera : BaseDevice, ICamera
    {
        private readonly int _deviceIndex;
        private VideoCapture _capture;
        private bool _isStreaming = false;
        private long _frameCounter = 0;
        private CancellationTokenSource _streamingCts;
        private readonly object _captureLock = new object();

        public override DeviceType DeviceType => DeviceType.Camera;

        public CameraSettings Settings { get; set; } = new CameraSettings();

        public event EventHandler<FrameCapturedEventArgs> FrameCaptured;

        public int DeviceIndex => _deviceIndex;

        public LocalCamera(int deviceIndex, string deviceId = null, string deviceName = null)
            : base(deviceId ?? $"LOCAL_CAM_{deviceIndex}", deviceName ?? $"Local Camera #{deviceIndex}", new NullConnection())
        {
            _deviceIndex = deviceIndex;
        }

        protected override Task OnInitializeAsync(CancellationToken cancellationToken)
        {
            EnsureCaptureCreated();
            ApplySettingsInternal();
            return Task.CompletedTask;
        }

        protected override Task<DeviceInfo> OnGetDeviceInfoAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(new DeviceInfo
            {
                DeviceId = DeviceId,
                DeviceName = DeviceName,
                DeviceType = DeviceType.Camera,
                Manufacturer = "Local",
                Model = "OpenCv VideoCapture",
                FirmwareVersion = "N/A",
                SerialNumber = _deviceIndex.ToString()
            });
        }

        protected override async Task OnResetAsync(CancellationToken cancellationToken)
        {
            await StopStreamingAsync(cancellationToken);
            lock (_captureLock)
            {
                _capture?.Release();
                _capture?.Dispose();
                _capture = null;
            }
            EnsureCaptureCreated();
            ApplySettingsInternal();
        }

        public Task<bool> StartStreamingAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfNotReady();
            if (_isStreaming)
                return Task.FromResult(true);

            EnsureCaptureCreated();

            _streamingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _isStreaming = true;
            _ = Task.Run(async () => await StreamingLoopAsync(_streamingCts.Token), _streamingCts.Token);
            return Task.FromResult(true);
        }

        public Task<bool> StopStreamingAsync(CancellationToken cancellationToken = default)
        {
            if (!_isStreaming)
                return Task.FromResult(true);

            _streamingCts.Cancel();
            _isStreaming = false;
            return Task.FromResult(true);
        }

        public Task<Resolution[]> GetSupportedResolutionsAsync(CancellationToken cancellationToken = default)
        {
            // Standard resolutions are often supported; exact list depends on driver
            var common = new[]
            {
                new Resolution(640, 480),
                new Resolution(1280, 720),
                new Resolution(1920, 1080)
            };
            return Task.FromResult(common);
        }

        public async Task<CameraFrame> CaptureFrameAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfNotReady();
            EnsureCaptureCreated();

            Mat mat = null;
            try
            {
                mat = new Mat();
                bool ok;
                lock (_captureLock)
                {
                    ok = _capture.Read(mat);
                }
                if (!ok || mat.Empty())
                    throw new InvalidOperationException("Не удалось получить кадр от камеры");

                var encoded = mat.ImEncode(Settings.Format == ImageFormat.PNG ? ".png" : ".jpg");
                var data = encoded.ToArray();

                var frame = new CameraFrame
                {
                    Data = data,
                    Resolution = new Resolution(mat.Width, mat.Height),
                    Format = Settings.Format == ImageFormat.PNG ? ImageFormat.PNG : ImageFormat.JPEG,
                    Timestamp = DateTime.Now,
                    FrameNumber = Interlocked.Increment(ref _frameCounter)
                };

                return frame;
            }
            finally
            {
                mat?.Dispose();
            }
        }

        public Task<bool> SaveFrameAsync(CameraFrame frame, string filePath, CancellationToken cancellationToken = default)
        {
            if (frame == null)
                throw new ArgumentNullException(nameof(frame));
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Путь к файлу не может быть пустым", nameof(filePath));
            return File.WriteAllBytesAsync(filePath, frame.Data, cancellationToken)
                .ContinueWith(_ => true, cancellationToken);
        }

        private async Task StreamingLoopAsync(CancellationToken cancellationToken)
        {
            var delayMs = Math.Max(1, 1000 / Math.Max(1, Settings.FrameRate));
            while (!cancellationToken.IsCancellationRequested && _isStreaming)
            {
                try
                {
                    var frame = await CaptureFrameAsync(cancellationToken);
                    OnFrameCaptured(new FrameCapturedEventArgs { Frame = frame });
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    break;
                }
                await Task.Delay(delayMs, cancellationToken);
            }
            _isStreaming = false;
        }

        private void EnsureCaptureCreated()
        {
            lock (_captureLock)
            {
                if (_capture == null)
                {
                    _capture = new VideoCapture(_deviceIndex);
                    if (!_capture.IsOpened())
                        throw new InvalidOperationException($"Камера с индексом {_deviceIndex} недоступна");
                }
            }
        }

        private void ApplySettingsInternal()
        {
            lock (_captureLock)
            {
                if (_capture == null)
                    return;
                _capture.Set(VideoCaptureProperties.FrameWidth, Settings.Resolution.Width);
                _capture.Set(VideoCaptureProperties.FrameHeight, Settings.Resolution.Height);
                _capture.Set(VideoCaptureProperties.Fps, Settings.FrameRate);
            }
        }

        protected virtual void OnFrameCaptured(FrameCapturedEventArgs e)
        {
            FrameCaptured?.Invoke(this, e);
        }

        public override void Dispose()
        {
            if (!_disposed)
            {
                try { StopStreamingAsync().Wait(); } catch { }
                _streamingCts?.Dispose();
                lock (_captureLock)
                {
                    _capture?.Release();
                    _capture?.Dispose();
                    _capture = null;
                }
            }
            base.Dispose();
        }
    }
}

