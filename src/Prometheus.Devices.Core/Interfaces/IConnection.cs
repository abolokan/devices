namespace Prometheus.Devices.Core.Interfaces
{
    /// <summary>
    /// Base interface for all types of device connections
    /// </summary>
    public interface IConnection : IDisposable
    {
        /// <summary>
        /// Connection status
        /// </summary>
        ConnectionStatus Status { get; }

        /// <summary>
        /// Connection information
        /// </summary>
        string ConnectionInfo { get; }

        /// <summary>
        /// Connection status changed event
        /// </summary>
        event EventHandler<ConnectionStatusChangedEventArgs> StatusChanged;

        /// <summary>
        /// Open connection
        /// </summary>
        Task OpenAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Close connection
        /// </summary>
        Task CloseAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Send data
        /// </summary>
        Task<int> SendAsync(byte[] data, CancellationToken cancellationToken = default);

        /// <summary>
        /// Receive data
        /// </summary>
        Task<byte[]> ReceiveAsync(int bufferSize = 4096, CancellationToken cancellationToken = default);

        /// <summary>
        /// Check connection availability
        /// </summary>
        Task<bool> PingAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Connection status
    /// </summary>
    public enum ConnectionStatus
    {
        Disconnected,
        Connecting,
        Connected,
        Disconnecting,
        Error
    }

    /// <summary>
    /// Connection status changed event arguments
    /// </summary>
    public class ConnectionStatusChangedEventArgs : EventArgs
    {
        public ConnectionStatus OldStatus { get; set; }
        public ConnectionStatus NewStatus { get; set; }
        public string Message { get; set; }
        public Exception Error { get; set; }
    }
}
