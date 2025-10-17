using Prometheus.Devices.Core.Interfaces;
using Prometheus.Devices.Core.Utils;

namespace Prometheus.Devices.Core.Devices
{
    /// <summary>
    /// Base abstract device implementation with retry support
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

        protected virtual RetryPolicy RetryPolicy { get; set; }

        protected BaseDevice(string deviceId, string deviceName, IConnection connection)
        {
            DeviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
            DeviceName = deviceName ?? throw new ArgumentNullException(nameof(deviceName));
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
            RetryPolicy = CreateDefaultRetryPolicy();
        }

        protected virtual RetryPolicy CreateDefaultRetryPolicy()
        {
            return new RetryPolicy
            {
                MaxRetries = 3,
                DelayMs = 1000,
                ExponentialBackoff = true
            };
        }

        public virtual async Task<bool> InitializeAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            try
            {
                SetStatus(DeviceStatus.Initializing, "Initializing device...");
                
                await OnInitializeAsync(cancellationToken);
                
                SetStatus(DeviceStatus.Ready, "Device ready");
                return true;
            }
            catch (Exception ex)
            {
                SetStatus(DeviceStatus.Error, $"Initialization error: {ex.Message}");
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

                await RetryPolicy.ExecuteAsync(async () =>
                {
                    await Connection.OpenAsync(cancellationToken);
                }, cancellationToken);
                
                if (Status == DeviceStatus.NotInitialized)
                    return await InitializeAsync(cancellationToken);

                SetStatus(DeviceStatus.Ready, "Connected to device");
                return true;
            }
            catch (Exception ex)
            {
                SetStatus(DeviceStatus.Error, $"Connection error: {ex.Message}");
                return false;
            }
        }

        public virtual async Task<bool> DisconnectAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                SetStatus(DeviceStatus.Disconnected, "Disconnecting from device...");
                await Connection.CloseAsync(cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                SetStatus(DeviceStatus.Error, $"Disconnection error: {ex.Message}");
                return false;
            }
        }

        public virtual async Task<DeviceInfo> GetDeviceInfoAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (Status != DeviceStatus.Ready && Status != DeviceStatus.Busy)
                throw new InvalidOperationException("Device not ready");

            return await RetryPolicy.ExecuteAsync(
                async () => await OnGetDeviceInfoAsync(cancellationToken),
                cancellationToken);
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
                throw new InvalidOperationException($"Device not ready. Current status: {Status}");
        }

        public virtual void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            try
            {
                DisconnectAsync().Wait();
            }
            catch
            {
            }
            Connection?.Dispose();
        }
    }
}

