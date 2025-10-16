using Prometheus.Devices.Core.Connections;

namespace DeviceWrappers.Devices.Printer
{
    /// <summary>
    /// Сетевой принтер (подключение через TCP)
    /// </summary>
    public class NetworkPrinter : GenericPrinter
    {
        public string IpAddress { get; }
        public int Port { get; }

        public NetworkPrinter(string deviceId, string deviceName, string ipAddress, int port = 9100)
            : base(deviceId, deviceName, new TcpConnection(ipAddress, port))
        {
            IpAddress = ipAddress;
            Port = port;
        }

        public static NetworkPrinter Create(string ipAddress, int port = 9100, string name = null)
        {
            string deviceId = $"NET_PRINTER_{ipAddress}_{port}";
            string deviceName = name ?? $"Network Printer ({ipAddress})";
            return new NetworkPrinter(deviceId, deviceName, ipAddress, port);
        }
    }
}

