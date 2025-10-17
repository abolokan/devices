using Prometheus.Devices.Core.Interfaces;

namespace Prometheus.Devices.Core.Connections
{
    /// <summary>
    /// Base abstract connection implementation
    /// Provides common functionality for all connection types
    /// Thread-safe: Status changes are synchronized
    /// </summary>
    public abstract class BaseConnection : IConnection
    {
        protected ConnectionStatus _status = ConnectionStatus.Disconnected;
        protected readonly object _statusLock = new();
        protected bool _disposed = false;

        /// <summary>
        /// Current connection status (thread-safe)
        /// </summary>
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

        /// <summary>
        /// Connection information string
        /// </summary>
        public abstract string ConnectionInfo { get; }

        /// <summary>
        /// Connection status changed event
        /// Initialize with empty delegate to avoid null checks
        /// </summary>
        public event EventHandler<ConnectionStatusChangedEventArgs> StatusChanged = delegate { };

        public abstract Task OpenAsync(CancellationToken cancellationToken = default);
        public abstract Task CloseAsync(CancellationToken cancellationToken = default);
        public abstract Task<int> SendAsync(byte[] data, CancellationToken cancellationToken = default);
        public abstract Task<byte[]> ReceiveAsync(int bufferSize = 4096, CancellationToken cancellationToken = default);
        public abstract Task<bool> PingAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Set connection status and fire event (thread-safe)
        /// </summary>
        protected void SetStatus(ConnectionStatus newStatus, string? message = null, Exception? error = null)
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

        /// <summary>
        /// Raise status changed event
        /// </summary>
        protected virtual void OnStatusChanged(ConnectionStatusChangedEventArgs e)
        {
            StatusChanged.Invoke(this, e);
        }

        /// <summary>
        /// Throw if object is disposed
        /// </summary>
        protected void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);
        }

        /// <summary>
        /// Dispose connection resources
        /// Note: Calls CloseAsync synchronously - override Dispose in derived classes if needed
        /// </summary>
        public virtual void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            
            // Close connection synchronously
            // Use try-catch to prevent exceptions in Dispose
            try
            {
                CloseAsync().GetAwaiter().GetResult();
            }
            catch
            {
                // Suppress exceptions in Dispose
            }
        }
    }
}
