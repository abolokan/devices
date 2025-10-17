using Prometheus.Devices.Connections;

namespace Prometheus.Devices.Printers
{
    /// <summary>
    /// Printer with COM port (Serial) connection
    /// Often used for receipt printers
    /// </summary>
    public class SerialPrinter : GenericPrinter
    {
        public string PortName { get; }
        public int BaudRate { get; }

        private SerialPrinter(string deviceId, string deviceName, string portName, int baudRate = 9600)
            : base(deviceId, deviceName, new SerialConnection(portName, baudRate))
        {
            PortName = portName;
            BaudRate = baudRate;
        }

        public static SerialPrinter Create(string portName, int baudRate = 9600, string name = null)
        {
            string deviceId = $"SERIAL_PRINTER_{portName}";
            string deviceName = name ?? $"Serial Printer ({portName})";
            return new SerialPrinter(deviceId, deviceName, portName, baudRate);
        }
    }
}

