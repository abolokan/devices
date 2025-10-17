namespace Prometheus.Devices.Connections
{
    /// <summary>
    /// Connection exception
    /// </summary>
    public class ConnectionException : Exception
    {
        public ConnectionException(string message) : base(message) { }
        public ConnectionException(string message, Exception innerException) : base(message, innerException) { }
    }
}

