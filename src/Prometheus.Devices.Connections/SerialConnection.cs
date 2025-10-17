using System.IO.Ports;
using Prometheus.Devices.Abstractions.Interfaces;
using Prometheus.Devices.Core.Connections;

namespace Prometheus.Devices.Connections
{
    /// <summary>
    /// Production-ready Serial (COM port) connection implementation
    /// Cross-platform: Windows (COM1, COM2...), Linux (/dev/ttyUSB0, /dev/ttyS0...)
    /// Features: Configurable timeouts, handshake control, auto-flush
    /// </summary>
    public class SerialConnection : BaseConnection
    {
        private readonly string _portName;
        private readonly int _baudRate;
        private readonly Parity _parity;
        private readonly int _dataBits;
        private readonly StopBits _stopBits;
        private readonly Handshake _handshake;
        private readonly int _readTimeoutMs;
        private readonly int _writeTimeoutMs;
        
        private SerialPort? _serialPort;

        public override string ConnectionInfo => $"Serial: {_portName} ({_baudRate} baud, {_parity}, {_dataBits}{_stopBits})";

        public string PortName => _portName;
        public int BaudRate => _baudRate;

        /// <summary>
        /// Create Serial connection with production settings
        /// </summary>
        /// <param name="portName">Port name (Windows: COM1, COM2... | Linux: /dev/ttyUSB0, /dev/ttyS0...)</param>
        /// <param name="baudRate">Baud rate (default: 9600, typical: 9600, 19200, 38400, 115200)</param>
        /// <param name="parity">Parity (default: None)</param>
        /// <param name="dataBits">Data bits (default: 8)</param>
        /// <param name="stopBits">Stop bits (default: One)</param>
        /// <param name="handshake">Handshake protocol (default: None, use RequestToSend for hardware flow control)</param>
        /// <param name="readTimeoutMs">Read timeout (default: 5000ms)</param>
        /// <param name="writeTimeoutMs">Write timeout (default: 5000ms)</param>
        public SerialConnection(
            string portName,
            int baudRate = 9600,
            Parity parity = Parity.None,
            int dataBits = 8,
            StopBits stopBits = StopBits.One,
            Handshake handshake = Handshake.None,
            int readTimeoutMs = 5000,
            int writeTimeoutMs = 5000)
        {
            _portName = portName ?? throw new ArgumentNullException(nameof(portName));
            _baudRate = baudRate;
            _parity = parity;
            _dataBits = dataBits;
            _stopBits = stopBits;
            _handshake = handshake;
            _readTimeoutMs = readTimeoutMs;
            _writeTimeoutMs = writeTimeoutMs;

            if (baudRate <= 0)
                throw new ArgumentException("Baud rate must be positive", nameof(baudRate));

            if (string.IsNullOrWhiteSpace(portName))
                throw new ArgumentException("Port name cannot be empty", nameof(portName));
        }

        public override async Task OpenAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (Status == ConnectionStatus.Connected)
                return;

            await Task.Run(() =>
            {
                try
                {
                    SetStatus(ConnectionStatus.Connecting, $"Opening port {_portName}...");

                    _serialPort = new SerialPort(_portName, _baudRate, _parity, _dataBits, _stopBits)
                    {
                        ReadTimeout = _readTimeoutMs,
                        WriteTimeout = _writeTimeoutMs,
                        Handshake = _handshake,
                        
                        // DTR/RTS control
                        DtrEnable = _handshake == Handshake.None || _handshake == Handshake.RequestToSend,
                        RtsEnable = _handshake == Handshake.None || _handshake == Handshake.RequestToSend,
                        
                        // Encoding and newline
                        Encoding = System.Text.Encoding.ASCII,
                        NewLine = "\r\n",
                        
                        // Buffer sizes
                        ReadBufferSize = 4096,
                        WriteBufferSize = 4096,
                        
                        // Disable events (using async I/O instead)
                        ReceivedBytesThreshold = 1
                    };

                    _serialPort.Open();
                    
                    // Clear buffers after opening
                    _serialPort.DiscardInBuffer();
                    _serialPort.DiscardOutBuffer();

                    SetStatus(ConnectionStatus.Connected, $"Port {_portName} opened");
                }
                catch (UnauthorizedAccessException ex)
                {
                    SetStatus(ConnectionStatus.Error, "Port access denied", ex);
                    throw new ConnectionException($"Access denied to port {_portName}. On Linux: check permissions or add user to dialout group.", ex);
                }
                catch (IOException ex) when (ex.Message.Contains("does not exist"))
                {
                    SetStatus(ConnectionStatus.Error, "Port not found", ex);
                    throw new ConnectionException($"Port {_portName} not found. Available ports: {string.Join(", ", GetAvailablePorts())}", ex);
                }
                catch (Exception ex)
                {
                    SetStatus(ConnectionStatus.Error, "Error opening COM port", ex);
                    throw new ConnectionException($"Failed to open port {_portName}", ex);
                }
            }, cancellationToken);
        }

        public override async Task CloseAsync(CancellationToken cancellationToken = default)
        {
            if (Status == ConnectionStatus.Disconnected)
                return;

            await Task.Run(() =>
            {
                try
                {
                    SetStatus(ConnectionStatus.Disconnecting, "Closing COM port...");
                    Cleanup();
                    SetStatus(ConnectionStatus.Disconnected, "COM port closed");
                }
                catch (Exception ex)
                {
                    SetStatus(ConnectionStatus.Error, "Error closing COM port", ex);
                    throw;
                }
            }, cancellationToken);
        }

        public override async Task<int> SendAsync(byte[] data, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (Status != ConnectionStatus.Connected)
                throw new InvalidOperationException("COM port is not open");

            if (data == null || data.Length == 0)
                throw new ArgumentException("Data cannot be empty", nameof(data));

            if (_serialPort == null)
                throw new InvalidOperationException("Serial port not initialized");

            try
            {
                await _serialPort.BaseStream.WriteAsync(data, 0, data.Length, cancellationToken);
                await _serialPort.BaseStream.FlushAsync(cancellationToken);
                return data.Length;
            }
            catch (TimeoutException ex)
            {
                SetStatus(ConnectionStatus.Error, $"Write timeout ({_writeTimeoutMs}ms)", ex);
                throw new ConnectionException($"Write timeout after {_writeTimeoutMs}ms", ex);
            }
            catch (IOException ex)
            {
                SetStatus(ConnectionStatus.Error, "I/O error writing to COM port", ex);
                throw new ConnectionException("Error sending data through COM port", ex);
            }
            catch (Exception ex)
            {
                SetStatus(ConnectionStatus.Error, "Error sending data through COM port", ex);
                throw new ConnectionException("Error sending data through COM port", ex);
            }
        }

        public override async Task<byte[]> ReceiveAsync(int bufferSize = 4096, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (Status != ConnectionStatus.Connected)
                throw new InvalidOperationException("COM port is not open");

            if (_serialPort == null)
                throw new InvalidOperationException("Serial port not initialized");

            try
            {
                var buffer = new byte[bufferSize];
                var bytesRead = await _serialPort.BaseStream.ReadAsync(buffer, 0, bufferSize, cancellationToken);

                if (bytesRead == 0)
                    return Array.Empty<byte>();

                var result = new byte[bytesRead];
                Array.Copy(buffer, result, bytesRead);
                return result;
            }
            catch (TimeoutException ex)
            {
                // Timeout on read is often OK (no data available)
                SetStatus(ConnectionStatus.Error, $"Read timeout ({_readTimeoutMs}ms)", ex);
                throw new ConnectionException($"Read timeout after {_readTimeoutMs}ms", ex);
            }
            catch (IOException ex)
            {
                SetStatus(ConnectionStatus.Error, "I/O error reading from COM port", ex);
                throw new ConnectionException("Error receiving data through COM port", ex);
            }
            catch (Exception ex)
            {
                SetStatus(ConnectionStatus.Error, "Error receiving data through COM port", ex);
                throw new ConnectionException("Error receiving data through COM port", ex);
            }
        }

        public override async Task<bool> PingAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return _serialPort != null && _serialPort.IsOpen;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Check if data is available to read (non-blocking)
        /// </summary>
        public int BytesToRead => _serialPort?.BytesToRead ?? 0;

        /// <summary>
        /// Reconnect to serial port
        /// </summary>
        public async Task ReconnectAsync(CancellationToken cancellationToken = default)
        {
            await CloseAsync(cancellationToken);
            await Task.Delay(1000, cancellationToken);
            await OpenAsync(cancellationToken);
        }

        /// <summary>
        /// Get list of available COM ports (cross-platform)
        /// Windows: COM1, COM2, COM3...
        /// Linux: /dev/ttyUSB0, /dev/ttyS0, /dev/ttyACM0...
        /// </summary>
        public static string[] GetAvailablePorts()
        {
            return SerialPort.GetPortNames();
        }

        /// <summary>
        /// Normalize port name for current platform
        /// </summary>
        public static string NormalizePortName(string portName)
        {
            if (string.IsNullOrWhiteSpace(portName))
                throw new ArgumentException("Port name cannot be empty");

            // Already in correct format
            if (portName.StartsWith("/dev/") || portName.StartsWith("COM", StringComparison.OrdinalIgnoreCase))
                return portName;

            // Convert number to platform-specific format
            if (int.TryParse(portName, out int portNumber))
            {
                // Linux: /dev/ttyUSB0, /dev/ttyUSB1...
                if (OperatingSystem.IsLinux())
                    return $"/dev/ttyUSB{portNumber}";
                
                // Windows: COM1, COM2...
                return $"COM{portNumber}";
            }

            return portName;
        }

        private void Cleanup()
        {
            try
            {
                if (_serialPort != null && _serialPort.IsOpen)
                {
                    _serialPort.DiscardInBuffer();
                    _serialPort.DiscardOutBuffer();
                    _serialPort.Close();
                }
            }
            catch { }

            try
            {
                _serialPort?.Dispose();
                _serialPort = null;
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
}


