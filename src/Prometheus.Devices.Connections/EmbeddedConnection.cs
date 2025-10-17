using Prometheus.Devices.Abstractions.Interfaces;
using Prometheus.Devices.Core.Connections;

namespace Prometheus.Devices.Connections
{
    /// <summary>
    /// Embedded connection for devices that don't require direct transport layer
    /// Used by devices that are accessed through OS APIs or built-in hardware
    /// Examples: LocalCamera (OpenCV/V4L2), OfficePrinter (CUPS/PrintDocument), OfficeScanner (SANE/WIA)
    /// Cross-platform: Works on Windows, Linux, macOS
    /// </summary>
    public class EmbeddedConnection : BaseConnection
    {
        public override string ConnectionInfo => "Embedded";

        /// <summary>
        /// Open connection (no-op for embedded devices)
        /// </summary>
        public override Task OpenAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            
            if (Status == ConnectionStatus.Connected)
                return Task.CompletedTask;
                
            SetStatus(ConnectionStatus.Connected, "Embedded connection ready");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Close connection (no-op for embedded devices)
        /// </summary>
        public override Task CloseAsync(CancellationToken cancellationToken = default)
        {
            if (Status == ConnectionStatus.Disconnected)
                return Task.CompletedTask;
                
            SetStatus(ConnectionStatus.Disconnected, "Embedded connection closed");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Send data (not supported - embedded devices use OS APIs)
        /// </summary>
        public override Task<int> SendAsync(byte[] data, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException(
                "EmbeddedConnection does not support direct data transfer. " +
                "Device communication happens through OS APIs (OpenCV, CUPS, SANE, etc.)");
        }

        /// <summary>
        /// Receive data (not supported - embedded devices use OS APIs)
        /// </summary>
        public override Task<byte[]> ReceiveAsync(int bufferSize = 4096, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException(
                "EmbeddedConnection does not support direct data transfer. " +
                "Device communication happens through OS APIs (OpenCV, CUPS, SANE, etc.)");
        }

        /// <summary>
        /// Ping (always returns true for embedded devices)
        /// </summary>
        public override Task<bool> PingAsync(CancellationToken cancellationToken = default)
        {
            // Embedded devices are "always available" from connection perspective
            // Actual availability is checked by device implementation (e.g., camera exists, printer in system)
            return Task.FromResult(true);
        }
    }
}

