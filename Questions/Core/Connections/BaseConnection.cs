using System;
using System.Threading;
using System.Threading.Tasks;
using DeviceWrappers.Core.Interfaces;

namespace DeviceWrappers.Core.Connections
{
    /// <summary>
    /// Базовая абстрактная реализация подключения
    /// </summary>
    public abstract class BaseConnection : IConnection
    {
        protected ConnectionStatus _status = ConnectionStatus.Disconnected;
        protected readonly object _statusLock = new object();
        protected bool _disposed = false;

        public ConnectionStatus Status
        {
            get
            {
                lock (_statusLock)
                {
                    return _status;
                }
            }
        }

        public abstract string ConnectionInfo { get; }

        public event EventHandler<ConnectionStatusChangedEventArgs> StatusChanged;

        public abstract Task OpenAsync(CancellationToken cancellationToken = default);
        public abstract Task CloseAsync(CancellationToken cancellationToken = default);
        public abstract Task<int> SendAsync(byte[] data, CancellationToken cancellationToken = default);
        public abstract Task<byte[]> ReceiveAsync(int bufferSize = 4096, CancellationToken cancellationToken = default);
        public abstract Task<bool> PingAsync(CancellationToken cancellationToken = default);

        protected void SetStatus(ConnectionStatus newStatus, string message = null, Exception error = null)
        {
            ConnectionStatus oldStatus;
            lock (_statusLock)
            {
                oldStatus = _status;
                if (oldStatus == newStatus)
                    return;
                _status = newStatus;
            }

            OnStatusChanged(new ConnectionStatusChangedEventArgs
            {
                OldStatus = oldStatus,
                NewStatus = newStatus,
                Message = message,
                Error = error
            });
        }

        protected virtual void OnStatusChanged(ConnectionStatusChangedEventArgs e)
        {
            StatusChanged?.Invoke(this, e);
        }

        protected void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);
        }

        public virtual void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            CloseAsync().Wait();
        }
    }
}

