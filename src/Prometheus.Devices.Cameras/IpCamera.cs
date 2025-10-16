using Prometheus.Devices.Core.Connections;

namespace DeviceWrappers.Devices.Camera
{
    /// <summary>
    /// IP camera (TCP connection)
    /// </summary>
    public class IpCamera : GenericCamera
    {
        public string IpAddress { get; }
        public int Port { get; }

        public IpCamera(string deviceId, string deviceName, string ipAddress, int port)
            : base(deviceId, deviceName, new TcpConnection(ipAddress, port))
        {
            IpAddress = ipAddress;
            Port = port;
        }

        public static IpCamera Create(string ipAddress, int port, string name = null)
        {
            string deviceId = $"IP_{ipAddress}_{port}";
            string deviceName = name ?? $"IP Camera ({ipAddress})";
            return new IpCamera(deviceId, deviceName, ipAddress, port);
        }
    }
}
