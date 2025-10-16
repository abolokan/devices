using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DeviceWrappers.Core.Devices;
using DeviceWrappers.Core.Interfaces;

namespace DeviceWrappers.Devices.Camera
{
    /// <summary>
    /// Общая реализация камеры
    /// </summary>
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
            // Отправляем команду инициализации камере
            byte[] initCommand = System.Text.Encoding.ASCII.GetBytes("INIT\r\n");
            await Connection.SendAsync(initCommand, cancellationToken);

            // Ожидаем ответа
            await Task.Delay(500, cancellationToken);

            // Настраиваем параметры по умолчанию
            await ApplySettingsAsync(cancellationToken);
        }

        protected override async Task<DeviceInfo> OnGetDeviceInfoAsync(CancellationToken cancellationToken)
        {
            // Запрашиваем информацию об устройстве
            byte[] command = System.Text.Encoding.ASCII.GetBytes("GET_INFO\r\n");
            await Connection.SendAsync(command, cancellationToken);

            byte[] response = await Connection.ReceiveAsync(1024, cancellationToken);
            string responseStr = System.Text.Encoding.ASCII.GetString(response);

            // Парсим ответ (упрощенный вариант)
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
                SetStatus(DeviceStatus.Busy, "Захват кадра...");

                // Отправляем команду захвата кадра
                byte[] captureCommand = System.Text.Encoding.ASCII.GetBytes("CAPTURE\r\n");
                await Connection.SendAsync(captureCommand, cancellationToken);

                // Получаем размер изображения
                byte[] sizeData = await Connection.ReceiveAsync(4, cancellationToken);
                int imageSize = BitConverter.ToInt32(sizeData, 0);

                // Получаем данные изображения
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

                SetStatus(DeviceStatus.Ready, "Кадр захвачен");
                return frame;
            }
            catch (Exception ex)
            {
                SetStatus(DeviceStatus.Error, $"Ошибка захвата кадра: {ex.Message}");
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

                // Отправляем команду начала потоковой передачи
                byte[] streamCommand = System.Text.Encoding.ASCII.GetBytes("START_STREAM\r\n");
                await Connection.SendAsync(streamCommand, cancellationToken);

                // Запускаем фоновую задачу для получения кадров
                _ = Task.Run(async () => await StreamingLoopAsync(_streamingCts.Token), _streamingCts.Token);

                return true;
            }
            catch (Exception ex)
            {
                _isStreaming = false;
                SetStatus(DeviceStatus.Error, $"Ошибка запуска потока: {ex.Message}");
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

                // Отправляем команду остановки потоковой передачи
                byte[] stopCommand = System.Text.Encoding.ASCII.GetBytes("STOP_STREAM\r\n");
                await Connection.SendAsync(stopCommand, cancellationToken);

                return true;
            }
            catch (Exception ex)
            {
                SetStatus(DeviceStatus.Error, $"Ошибка остановки потока: {ex.Message}");
                return false;
            }
        }

        public async Task<Resolution[]> GetSupportedResolutionsAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfNotReady();

            // В реальной реализации эти данные приходят от устройства
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
                throw new ArgumentException("Путь к файлу не может быть пустым", nameof(filePath));

            try
            {
                await File.WriteAllBytesAsync(filePath, frame.Data, cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                throw new IOException($"Не удалось сохранить кадр: {ex.Message}", ex);
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
                    SetStatus(DeviceStatus.Error, $"Ошибка в потоке видео: {ex.Message}");
                    break;
                }

                // Задержка для соблюдения частоты кадров
                int delayMs = 1000 / Settings.FrameRate;
                await Task.Delay(delayMs, cancellationToken);
            }

            _isStreaming = false;
        }

        private async Task ApplySettingsAsync(CancellationToken cancellationToken)
        {
            // Отправляем настройки камере
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

