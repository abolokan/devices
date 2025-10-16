using Prometheus.Devices.Core.Interfaces;

namespace Prometheus.Devices.Core.Devices
{
    /// <summary>
    /// Базовая абстрактная реализация устройства
    /// </summary>
    public abstract class BaseDevice : IDevice
    {
        protected DeviceStatus _status = DeviceStatus.NotInitialized;
        protected readonly object _statusLock = new object();
        protected bool _disposed = false;

        public string DeviceId { get; protected set; }
        public string DeviceName { get; protected set; }
        public abstract DeviceType DeviceType { get; }
        
        public DeviceStatus Status
        {
            get
            {
                lock (_statusLock)
                {
                    return _status;
                }
            }
        }

        public IConnection Connection { get; protected set; }

        public event EventHandler<DeviceStatusChangedEventArgs> StatusChanged;

        protected BaseDevice(string deviceId, string deviceName, IConnection connection)
        {
            DeviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
            DeviceName = deviceName ?? throw new ArgumentNullException(nameof(deviceName));
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        public virtual async Task<bool> InitializeAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            try
            {
                SetStatus(DeviceStatus.Initializing, "Инициализация устройства...");
                
                await OnInitializeAsync(cancellationToken);
                
                SetStatus(DeviceStatus.Ready, "Устройство готово к работе");
                return true;
            }
            catch (Exception ex)
            {
                SetStatus(DeviceStatus.Error, $"Ошибка инициализации: {ex.Message}");
                return false;
            }
        }

        public virtual async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            try
            {
                if (Connection.Status == ConnectionStatus.Connected)
                    return true;

                await Connection.OpenAsync(cancellationToken);
                
                if (Status == DeviceStatus.NotInitialized)
                    return await InitializeAsync(cancellationToken);

                SetStatus(DeviceStatus.Ready, "Подключено к устройству");
                return true;
            }
            catch (Exception ex)
            {
                SetStatus(DeviceStatus.Error, $"Ошибка подключения: {ex.Message}");
                return false;
            }
        }

        public virtual async Task<bool> DisconnectAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                SetStatus(DeviceStatus.Disconnected, "Отключение от устройства...");
                await Connection.CloseAsync(cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                SetStatus(DeviceStatus.Error, $"Ошибка отключения: {ex.Message}");
                return false;
            }
        }

        public virtual async Task<DeviceInfo> GetDeviceInfoAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (Status != DeviceStatus.Ready && Status != DeviceStatus.Busy)
                throw new InvalidOperationException("Устройство не готово к работе");

            return await OnGetDeviceInfoAsync(cancellationToken);
        }

        public virtual async Task<bool> ResetAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            try
            {
                await OnResetAsync(cancellationToken);
                await InitializeAsync(cancellationToken);
                return true;
            }
            catch
            {
                return false;
            }
        }

        protected abstract Task OnInitializeAsync(CancellationToken cancellationToken);
        protected abstract Task<DeviceInfo> OnGetDeviceInfoAsync(CancellationToken cancellationToken);
        protected abstract Task OnResetAsync(CancellationToken cancellationToken);

        protected void SetStatus(DeviceStatus newStatus, string message = null)
        {
            DeviceStatus oldStatus;
            lock (_statusLock)
            {
                oldStatus = _status;
                if (oldStatus == newStatus)
                    return;
                _status = newStatus;
            }

            OnStatusChanged(new DeviceStatusChangedEventArgs
            {
                OldStatus = oldStatus,
                NewStatus = newStatus,
                Message = message
            });
        }

        protected virtual void OnStatusChanged(DeviceStatusChangedEventArgs e)
        {
            StatusChanged?.Invoke(this, e);
        }

        protected void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);
        }

        protected void ThrowIfNotReady()
        {
            ThrowIfDisposed();
            if (Status != DeviceStatus.Ready && Status != DeviceStatus.Busy)
                throw new InvalidOperationException("Устройство не готово к работе");
        }

        public virtual void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            DisconnectAsync().Wait();
            Connection?.Dispose();
        }
    }
}

