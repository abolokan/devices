using System.Net.Sockets;
using Prometheus.Devices.Core.Interfaces;

namespace Prometheus.Devices.Core.Connections
{
    /// <summary>
    /// TCP connection implementation
    /// </summary>
    public class TcpConnection : BaseConnection
    {
        private readonly string _host;
        private readonly int _port;
        private TcpClient _client;
        private NetworkStream _stream;

        public override string ConnectionInfo => $"TCP: {_host}:{_port}";

        public TcpConnection(string host, int port)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _port = port;

            if (port <= 0 || port > 65535)
                throw new ArgumentException("Port must be between 1 and 65535", nameof(port));
        }

        public override async Task OpenAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (Status == ConnectionStatus.Connected)
                return;

            try
            {
                SetStatus(ConnectionStatus.Connecting, "Connecting to device...");

                _client = new TcpClient();
                await _client.ConnectAsync(_host, _port, cancellationToken);
                _stream = _client.GetStream();

                SetStatus(ConnectionStatus.Connected, "Successfully connected");
            }
            catch (Exception ex)
            {
                SetStatus(ConnectionStatus.Error, "Connection error", ex);
                throw new ConnectionException($"Failed to connect to {_host}:{_port}", ex);
            }
        }

        public override async Task CloseAsync(CancellationToken cancellationToken = default)
        {
            if (Status == ConnectionStatus.Disconnected)
                return;

            try
            {
                SetStatus(ConnectionStatus.Disconnecting, "Disconnecting from device...");

                if (_stream != null)
                {
                    await _stream.FlushAsync(cancellationToken);
                    _stream.Close();
                    _stream = null;
                }

                if (_client != null)
                {
                    _client.Close();
                    _client = null;
                }

                SetStatus(ConnectionStatus.Disconnected, "Disconnected");
            }
            catch (Exception ex)
            {
                SetStatus(ConnectionStatus.Error, "Disconnection error", ex);
                throw;
            }
        }

        public override async Task<int> SendAsync(byte[] data, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (Status != ConnectionStatus.Connected)
                throw new InvalidOperationException("Connection not established");

            if (data == null || data.Length == 0)
                throw new ArgumentException("Data cannot be empty", nameof(data));

            try
            {
                await _stream.WriteAsync(data, 0, data.Length, cancellationToken);
                await _stream.FlushAsync(cancellationToken);
                return data.Length;
            }
            catch (Exception ex)
            {
                SetStatus(ConnectionStatus.Error, "Send data error", ex);
                throw new ConnectionException("Error sending data", ex);
            }
        }

        public override async Task<byte[]> ReceiveAsync(int bufferSize = 4096, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (Status != ConnectionStatus.Connected)
                throw new InvalidOperationException("Connection not established");

            try
            {
                var buffer = new byte[bufferSize];
                var bytesRead = await _stream.ReadAsync(buffer, 0, bufferSize, cancellationToken);

                if (bytesRead == 0)
                {
                    SetStatus(ConnectionStatus.Disconnected, "Connection closed by remote host");
                    return Array.Empty<byte>();
                }

                var result = new byte[bytesRead];
                Array.Copy(buffer, result, bytesRead);
                return result;
            }
            catch (Exception ex)
            {
                SetStatus(ConnectionStatus.Error, "Receive data error", ex);
                throw new ConnectionException("Error receiving data", ex);
            }
        }

        public override async Task<bool> PingAsync(CancellationToken cancellationToken = default)
        {
            if (_client == null || !_client.Connected)
                return false;

            try
            {
                // Check socket availability
                return !(_client.Client.Poll(1, SelectMode.SelectRead) && _client.Available == 0);
            }
            catch
            {
                return false;
            }
        }

        public override void Dispose()
        {
            if (_disposed)
                return;

            _stream?.Dispose();
            _client?.Dispose();

            base.Dispose();
        }
    }

    /// <summary>
    /// Connection exception
    /// </summary>
    public class ConnectionException : Exception
    {
        public ConnectionException(string message) : base(message) { }
        public ConnectionException(string message, Exception innerException) : base(message, innerException) { }
    }
}
