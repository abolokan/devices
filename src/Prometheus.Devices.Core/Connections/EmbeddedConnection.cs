using Prometheus.Devices.Core.Interfaces;

namespace Prometheus.Devices.Core.Connections
{
    /// <summary>
    /// Embedded connection for devices that don't require transport (built-in camera, etc.)
    /// </summary>
    public class EmbeddedConnection : BaseConnection
    {
        public override string ConnectionInfo => "Embedded:null";

        public override Task OpenAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            SetStatus(ConnectionStatus.Connected, "Local connection active");
            return Task.CompletedTask;
        }

        public override Task CloseAsync(CancellationToken cancellationToken = default)
        {
            SetStatus(ConnectionStatus.Disconnected, "Local connection closed");
            return Task.CompletedTask;
        }

        public override Task<int> SendAsync(byte[] data, CancellationToken cancellationToken = default) => throw new NotSupportedException("EmbeddedConnection does not support sending data");

        public override Task<byte[]> ReceiveAsync(int bufferSize = 4096, CancellationToken cancellationToken = default) => throw new NotSupportedException("EmbeddedConnection does not support receiving data");

        public override Task<bool> PingAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);
    }
}
