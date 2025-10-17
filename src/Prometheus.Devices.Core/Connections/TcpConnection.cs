using System.Net.Sockets;
using Prometheus.Devices.Core.Interfaces;

namespace Prometheus.Devices.Core.Connections
{
    /// <summary>
    /// Production-ready TCP connection implementation
    /// Cross-platform: Fully compatible with Linux and Windows
    /// Features: Timeouts, Keep-Alive, Auto-reconnect support
    /// </summary>
    public class TcpConnection : BaseConnection
    {
        private readonly string _host;
        private readonly int _port;
        private readonly int _connectTimeoutMs;
        private readonly int _sendTimeoutMs;
        private readonly int _receiveTimeoutMs;
        private readonly bool _enableKeepAlive;
        
        private TcpClient? _client;
        private NetworkStream? _stream;

        public override string ConnectionInfo => $"TCP: {_host}:{_port}";

        public string Host => _host;
        public int Port => _port;

        /// <summary>
        /// Create TCP connection with production-ready settings
        /// </summary>
        /// <param name="host">Host name or IP address</param>
        /// <param name="port">TCP port (1-65535)</param>
        /// <param name="connectTimeoutMs">Connection timeout in milliseconds (default: 5000)</param>
        /// <param name="sendTimeoutMs">Send timeout in milliseconds (default: 10000)</param>
        /// <param name="receiveTimeoutMs">Receive timeout in milliseconds (default: 10000)</param>
        /// <param name="enableKeepAlive">Enable TCP Keep-Alive (default: true)</param>
        public TcpConnection(
            string host, 
            int port,
            int connectTimeoutMs = 5000,
            int sendTimeoutMs = 10000,
            int receiveTimeoutMs = 10000,
            bool enableKeepAlive = true)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _port = port;
            _connectTimeoutMs = connectTimeoutMs;
            _sendTimeoutMs = sendTimeoutMs;
            _receiveTimeoutMs = receiveTimeoutMs;
            _enableKeepAlive = enableKeepAlive;

            if (port <= 0 || port > 65535)
                throw new ArgumentException("Port must be between 1 and 65535", nameof(port));

            if (string.IsNullOrWhiteSpace(host))
                throw new ArgumentException("Host cannot be empty", nameof(host));
        }

        public override async Task OpenAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (Status == ConnectionStatus.Connected)
                return;

            try
            {
                SetStatus(ConnectionStatus.Connecting, $"Connecting to {_host}:{_port}...");

                _client = new TcpClient();
                
                // Set socket options for production
                _client.NoDelay = true;  // Disable Nagle algorithm for low latency
                _client.SendBufferSize = 8192;
                _client.ReceiveBufferSize = 8192;

                // Connect with timeout
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(_connectTimeoutMs);
                
                await _client.ConnectAsync(_host, _port, cts.Token);
                
                // Configure Keep-Alive (important for long-running connections)
                if (_enableKeepAlive)
                {
                    _client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                    
                    // Linux/Windows specific Keep-Alive settings
                    if (OperatingSystem.IsLinux() || OperatingSystem.IsWindows())
                    {
                        // Keep-Alive: send probe after 60s of inactivity, every 10s, 5 times
                        _client.Client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, 60);
                        _client.Client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, 10);
                        
                        if (OperatingSystem.IsLinux())
                        {
                            _client.Client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount, 5);
                        }
                    }
                }

                _stream = _client.GetStream();
                _stream.ReadTimeout = _receiveTimeoutMs;
                _stream.WriteTimeout = _sendTimeoutMs;

                SetStatus(ConnectionStatus.Connected, $"Connected to {_host}:{_port}");
            }
            catch (OperationCanceledException)
            {
                Cleanup();
                SetStatus(ConnectionStatus.Error, "Connection timeout", null);
                throw new ConnectionException($"Connection to {_host}:{_port} timed out after {_connectTimeoutMs}ms");
            }
            catch (SocketException ex)
            {
                Cleanup();
                SetStatus(ConnectionStatus.Error, "Socket error", ex);
                throw new ConnectionException($"Failed to connect to {_host}:{_port}: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                Cleanup();
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
                await Task.Run(() => Cleanup(), cancellationToken);
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

            if (_stream == null)
                throw new InvalidOperationException("Network stream not initialized");

            try
            {
                await _stream.WriteAsync(data, 0, data.Length, cancellationToken);
                await _stream.FlushAsync(cancellationToken);
                return data.Length;
            }
            catch (IOException ex) when (ex.InnerException is SocketException socketEx)
            {
                SetStatus(ConnectionStatus.Error, $"Network error: {socketEx.SocketErrorCode}", ex);
                throw new ConnectionException($"Network error sending data: {socketEx.SocketErrorCode}", ex);
            }
            catch (OperationCanceledException)
            {
                SetStatus(ConnectionStatus.Error, "Send timeout", null);
                throw new ConnectionException($"Send timed out after {_sendTimeoutMs}ms");
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

            if (_stream == null)
                throw new InvalidOperationException("Network stream not initialized");

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
            catch (IOException ex) when (ex.InnerException is SocketException socketEx)
            {
                SetStatus(ConnectionStatus.Error, $"Network error: {socketEx.SocketErrorCode}", ex);
                throw new ConnectionException($"Network error receiving data: {socketEx.SocketErrorCode}", ex);
            }
            catch (OperationCanceledException)
            {
                SetStatus(ConnectionStatus.Error, "Receive timeout", null);
                throw new ConnectionException($"Receive timed out after {_receiveTimeoutMs}ms");
            }
            catch (Exception ex)
            {
                SetStatus(ConnectionStatus.Error, "Receive data error", ex);
                throw new ConnectionException("Error receiving data", ex);
            }
        }

        public override async Task<bool> PingAsync(CancellationToken cancellationToken = default)
        {
            if (_client == null || _stream == null)
                return false;

            try
            {
                // Check if socket is still connected
                if (!_client.Connected)
                    return false;

                // Poll to check if socket is readable (connection alive)
                // SelectMode.SelectRead with timeout 0 checks if data available or connection closed
                bool isReadable = _client.Client.Poll(1000, SelectMode.SelectRead);
                bool hasData = _client.Available > 0;

                // If readable but no data = connection closed
                if (isReadable && !hasData)
                {
                    SetStatus(ConnectionStatus.Disconnected, "Connection lost");
                    return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Check if data is available to read (non-blocking)
        /// Useful for checking before ReceiveAsync to avoid blocking
        /// </summary>
        public bool DataAvailable => _stream?.DataAvailable ?? false;

        /// <summary>
        /// Reconnect to device (useful for error recovery)
        /// </summary>
        public async Task ReconnectAsync(CancellationToken cancellationToken = default)
        {
            await CloseAsync(cancellationToken);
            await Task.Delay(1000, cancellationToken); // Wait before reconnect
            await OpenAsync(cancellationToken);
        }

        private void Cleanup()
        {
            try
            {
                _stream?.Close();
                _stream?.Dispose();
                _stream = null;
            }
            catch { }

            try
            {
                _client?.Close();
                _client?.Dispose();
                _client = null;
            }
            catch { }
        }

        public override void Dispose()
        {
            if (_disposed)
                return;

            Cleanup();
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
