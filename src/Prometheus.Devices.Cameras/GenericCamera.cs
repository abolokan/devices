using Prometheus.Devices.Core.Devices;
using Prometheus.Devices.Core.Interfaces;

namespace Prometheus.Devices.Cameras
{
    public class GenericCamera : BaseDevice, ICamera
    {
        private CameraSettings _settings;
        private bool _isStreaming = false;
        private long _frameCounter = 0;
        private CancellationTokenSource _streamingCts;

        public override DeviceType DeviceType => DeviceType.Camera;

        public CameraSettings Settings
        {
            get => _settings;
            set
            {
                _settings = value ?? throw new ArgumentNullException(nameof(value));
                OnSettingsChanged();
            }
        }

        public event EventHandler<FrameCapturedEventArgs> FrameCaptured;

        public GenericCamera(string deviceId, string deviceName, IConnection connection)
            : base(deviceId, deviceName, connection)
        {
            _settings = new CameraSettings();
        }

        protected override async Task OnInitializeAsync(CancellationToken cancellationToken)
        {
            // Send initialization command to camera
            byte[] initCommand = System.Text.Encoding.ASCII.GetBytes("INIT\r\n");
            await Connection.SendAsync(initCommand, cancellationToken);

            // Wait for response
            await Task.Delay(500, cancellationToken);

            // Configure default parameters
            await ApplySettingsAsync(cancellationToken);
        }

        protected override async Task<DeviceInfo> OnGetDeviceInfoAsync(CancellationToken cancellationToken)
        {
            // Request device information
            byte[] command = System.Text.Encoding.ASCII.GetBytes("GET_INFO\r\n");
            await Connection.SendAsync(command, cancellationToken);

            byte[] response = await Connection.ReceiveAsync(1024, cancellationToken);
            string responseStr = System.Text.Encoding.ASCII.GetString(response);

            // Parse response (simplified)
            return new DeviceInfo
            {
                DeviceId = DeviceId,
                DeviceName = DeviceName,
                DeviceType = DeviceType.Camera,
                Manufacturer = "Generic",
                Model = "Camera Model",
                FirmwareVersion = "1.0.0",
                SerialNumber = DeviceId
            };
        }

        protected override async Task OnResetAsync(CancellationToken cancellationToken)
        {
            if (_isStreaming)
            {
                await StopStreamingAsync(cancellationToken);
            }

            byte[] resetCommand = System.Text.Encoding.ASCII.GetBytes("RESET\r\n");
            await Connection.SendAsync(resetCommand, cancellationToken);
            await Task.Delay(1000, cancellationToken);
        }

        public async Task<CameraFrame> CaptureFrameAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfNotReady();

            try
            {
                SetStatus(DeviceStatus.Busy, "Capturing frame...");

                // Send frame capture command
                byte[] captureCommand = System.Text.Encoding.ASCII.GetBytes("CAPTURE\r\n");
                await Connection.SendAsync(captureCommand, cancellationToken);

                // Get image size
                byte[] sizeData = await Connection.ReceiveAsync(4, cancellationToken);
                int imageSize = BitConverter.ToInt32(sizeData, 0);

                // Get image data
                byte[] imageData = new byte[imageSize];
                int totalReceived = 0;

                while (totalReceived < imageSize)
                {
                    byte[] chunk = await Connection.ReceiveAsync(Math.Min(8192, imageSize - totalReceived), cancellationToken);
                    Array.Copy(chunk, 0, imageData, totalReceived, chunk.Length);
                    totalReceived += chunk.Length;
                }

                var frame = new CameraFrame
                {
                    Data = imageData,
                    Resolution = Settings.Resolution,
                    Format = Settings.Format,
                    Timestamp = DateTime.Now,
                    FrameNumber = Interlocked.Increment(ref _frameCounter)
                };

                SetStatus(DeviceStatus.Ready, "Frame captured");
                return frame;
            }
            catch (Exception ex)
            {
                SetStatus(DeviceStatus.Error, $"Frame capture error: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> StartStreamingAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfNotReady();

            if (_isStreaming)
                return true;

            try
            {
                _streamingCts = new CancellationTokenSource();
                _isStreaming = true;

                // Send stream start command
                byte[] streamCommand = System.Text.Encoding.ASCII.GetBytes("START_STREAM\r\n");
                await Connection.SendAsync(streamCommand, cancellationToken);

                // Start background task for receiving frames
                _ = Task.Run(async () => await StreamingLoopAsync(_streamingCts.Token), _streamingCts.Token);

                return true;
            }
            catch (Exception ex)
            {
                _isStreaming = false;
                SetStatus(DeviceStatus.Error, $"Streaming start error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> StopStreamingAsync(CancellationToken cancellationToken = default)
        {
            if (!_isStreaming)
                return true;

            try
            {
                _streamingCts?.Cancel();
                _isStreaming = false;

                // Send stream stop command
                byte[] stopCommand = System.Text.Encoding.ASCII.GetBytes("STOP_STREAM\r\n");
                await Connection.SendAsync(stopCommand, cancellationToken);

                return true;
            }
            catch (Exception ex)
            {
                SetStatus(DeviceStatus.Error, $"Streaming stop error: {ex.Message}");
                return false;
            }
        }

        public async Task<Resolution[]> GetSupportedResolutionsAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfNotReady();

            // In real implementation, this data comes from the device
            return new[]
            {
                new Resolution(640, 480),
                new Resolution(1280, 720),
                new Resolution(1920, 1080),
                new Resolution(2560, 1440),
                new Resolution(3840, 2160)
            };
        }

        public async Task<bool> SaveFrameAsync(CameraFrame frame, string filePath, CancellationToken cancellationToken = default)
        {
            if (frame == null)
                throw new ArgumentNullException(nameof(frame));

            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be empty", nameof(filePath));

            try
            {
                await File.WriteAllBytesAsync(filePath, frame.Data, cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                throw new IOException($"Failed to save frame: {ex.Message}", ex);
            }
        }

        private async Task StreamingLoopAsync(CancellationToken cancellationToken)
        {
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
                catch (Exception ex)
                {
                    SetStatus(DeviceStatus.Error, $"Video stream error: {ex.Message}");
                    break;
                }

                // Delay to maintain frame rate
                int delayMs = Settings.FrameRate > 0 ? 1000 / Settings.FrameRate : 33;
                await Task.Delay(delayMs, cancellationToken);
            }

            _isStreaming = false;
        }

        private async Task ApplySettingsAsync(CancellationToken cancellationToken)
        {
            // Send settings to camera
            string settingsCommand = $"SET_RESOLUTION:{Settings.Resolution.Width}x{Settings.Resolution.Height}\r\n" +
                                   $"SET_FPS:{Settings.FrameRate}\r\n" +
                                   $"SET_BRIGHTNESS:{Settings.Brightness}\r\n";

            byte[] command = System.Text.Encoding.ASCII.GetBytes(settingsCommand);
            await Connection.SendAsync(command, cancellationToken);
        }

        private void OnSettingsChanged()
        {
            if (Status == DeviceStatus.Ready)
            {
                _ = ApplySettingsAsync(CancellationToken.None);
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
                StopStreamingAsync().Wait();
                _streamingCts?.Dispose();
            }

            base.Dispose();
        }
    }
}

