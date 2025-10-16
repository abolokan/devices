using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using DeviceWrappers.Core.Interfaces;

namespace DeviceWrappers.Core.Connections
{
    /// <summary>
    /// Реализация Serial (COM-порт) подключения
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
                    SetStatus(ConnectionStatus.Connecting, $"Открытие порта {_portName}...");

                    _serialPort = new SerialPort(_portName, _baudRate, _parity, _dataBits, _stopBits)
                    {
                        ReadTimeout = 5000,
                        WriteTimeout = 5000,
                        DtrEnable = true,
                        RtsEnable = true
                    };

                    _serialPort.Open();

                    SetStatus(ConnectionStatus.Connected, $"Порт {_portName} открыт");
                }
                catch (Exception ex)
                {
                    SetStatus(ConnectionStatus.Error, "Ошибка открытия COM-порта", ex);
                    throw new ConnectionException($"Не удалось открыть порт {_portName}", ex);
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
                    SetStatus(ConnectionStatus.Disconnecting, "Закрытие COM-порта...");

                    if (_serialPort != null && _serialPort.IsOpen)
                    {
                        _serialPort.Close();
                    }

                    _serialPort?.Dispose();
                    _serialPort = null;

                    SetStatus(ConnectionStatus.Disconnected, "COM-порт закрыт");
                }
                catch (Exception ex)
                {
                    SetStatus(ConnectionStatus.Error, "Ошибка при закрытии COM-порта", ex);
                    throw;
                }
            }, cancellationToken);
        }

        public override async Task<int> SendAsync(byte[] data, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (Status != ConnectionStatus.Connected)
                throw new InvalidOperationException("COM-порт не открыт");

            if (data == null || data.Length == 0)
                throw new ArgumentException("Данные не могут быть пустыми", nameof(data));

            try
            {
                await _serialPort.BaseStream.WriteAsync(data, 0, data.Length, cancellationToken);
                await _serialPort.BaseStream.FlushAsync(cancellationToken);
                return data.Length;
            }
            catch (Exception ex)
            {
                SetStatus(ConnectionStatus.Error, "Ошибка отправки данных через COM-порт", ex);
                throw new ConnectionException("Ошибка при отправке данных через COM-порт", ex);
            }
        }

        public override async Task<byte[]> ReceiveAsync(int bufferSize = 4096, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (Status != ConnectionStatus.Connected)
                throw new InvalidOperationException("COM-порт не открыт");

            try
            {
                byte[] buffer = new byte[bufferSize];
                int bytesRead = await _serialPort.BaseStream.ReadAsync(buffer, 0, bufferSize, cancellationToken);

                if (bytesRead == 0)
                    return Array.Empty<byte>();

                byte[] result = new byte[bytesRead];
                Array.Copy(buffer, result, bytesRead);
                return result;
            }
            catch (Exception ex)
            {
                SetStatus(ConnectionStatus.Error, "Ошибка получения данных через COM-порт", ex);
                throw new ConnectionException("Ошибка при получении данных через COM-порт", ex);
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
        /// Получить список доступных COM-портов
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

