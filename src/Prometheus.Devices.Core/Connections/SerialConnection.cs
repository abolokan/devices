using System.IO.Ports;
using Prometheus.Devices.Core.Interfaces;

namespace Prometheus.Devices.Core.Connections
{
    /// <summary>
    /// Serial (COM port) connection implementation
    /// </summary>
    public class SerialConnection : BaseConnection
    {
        private readonly string _portName;
        private readonly int _baudRate;
        private readonly Parity _parity;
        private readonly int _dataBits;
        private readonly StopBits _stopBits;
        private SerialPort _serialPort;

        public override string ConnectionInfo => $"Serial: {_portName} ({_baudRate} baud)";

        public SerialConnection(
            string portName,
            int baudRate = 9600,
            Parity parity = Parity.None,
            int dataBits = 8,
            StopBits stopBits = StopBits.One)
        {
            _portName = portName ?? throw new ArgumentNullException(nameof(portName));
            _baudRate = baudRate;
            _parity = parity;
            _dataBits = dataBits;
            _stopBits = stopBits;
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
                        ReadTimeout = 5000,
                        WriteTimeout = 5000,
                        DtrEnable = true,
                        RtsEnable = true
                    };

                    _serialPort.Open();

                    SetStatus(ConnectionStatus.Connected, $"Port {_portName} opened");
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

                    if (_serialPort != null && _serialPort.IsOpen)
                        _serialPort.Close();

                    _serialPort?.Dispose();
                    _serialPort = null;

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

            try
            {
                await _serialPort.BaseStream.WriteAsync(data, 0, data.Length, cancellationToken);
                await _serialPort.BaseStream.FlushAsync(cancellationToken);
                return data.Length;
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
        /// Get list of available COM ports
        /// </summary>
        public static string[] GetAvailablePorts()
        {
            return SerialPort.GetPortNames();
        }

        public override void Dispose()
        {
            if (_disposed)
                return;

            _serialPort?.Dispose();

            base.Dispose();
        }
    }
}
