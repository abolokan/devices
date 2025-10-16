using System.IO.Ports;
using DeviceWrappers.Core.Connections;

namespace DeviceWrappers.Devices.Printer
{
    /// <summary>
    /// Принтер с подключением через COM-порт (Serial)
    /// Часто используется для чековых принтеров
    /// </summary>
    public class SerialPrinter : GenericPrinter
    {
        public string PortName { get; }
        public int BaudRate { get; }

        public SerialPrinter(string deviceId, string deviceName, string portName, int baudRate = 9600)
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

        /// <summary>
        /// Получить список доступных COM-портов
        /// </summary>
        public static string[] GetAvailablePorts()
        {
            return SerialPort.GetPortNames();
        }
    }
}

