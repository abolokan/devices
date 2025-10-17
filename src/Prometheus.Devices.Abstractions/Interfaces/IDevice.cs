namespace Prometheus.Devices.Abstractions.Interfaces
{
    /// <summary>
    /// Base interface for all devices
    /// </summary>
    public interface IDevice : IDisposable
    {
        /// <summary>
        /// Unique device identifier
        /// </summary>
        string DeviceId { get; }

        /// <summary>
        /// Device name
        /// </summary>
        string DeviceName { get; }

        /// <summary>
        /// Device type
        /// </summary>
        DeviceType DeviceType { get; }

        /// <summary>
        /// Device status
        /// </summary>
        DeviceStatus Status { get; }

        /// <summary>
        /// Connection to the device
        /// </summary>
        IConnection Connection { get; }

        /// <summary>
        /// Device status changed event
        /// </summary>
        event EventHandler<DeviceStatusChangedEventArgs> StatusChanged;

        /// <summary>
        /// Initialize device
        /// </summary>
        Task<bool> InitializeAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Connect to device
        /// </summary>
        Task<bool> ConnectAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Disconnect from device
        /// </summary>
        Task<bool> DisconnectAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Get device information
        /// </summary>
        Task<DeviceInfo> GetDeviceInfoAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Reset device
        /// </summary>
        Task<bool> ResetAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Device type
    /// </summary>
    public enum DeviceType
    {
        Unknown,
        Camera,
        Printer,
        Scanner,
        BarcodeScanner,
        Sensor,
        Display,
        Other
    }

    /// <summary>
    /// Device status
    /// </summary>
    public enum DeviceStatus
    {
        NotInitialized,
        Initializing,
        Ready,
        Busy,
        Error,
        Disconnected
    }

    /// <summary>
    /// Device status changed event arguments
    /// </summary>
    public class DeviceStatusChangedEventArgs : EventArgs
    {
        public DeviceStatus OldStatus { get; set; }
        public DeviceStatus NewStatus { get; set; }
        public string Message { get; set; }
    }

    /// <summary>
    /// Device information
    /// </summary>
    public class DeviceInfo
    {
        public string DeviceId { get; set; }
        public string DeviceName { get; set; }
        public string Manufacturer { get; set; }
        public string Model { get; set; }
        public string FirmwareVersion { get; set; }
        public string SerialNumber { get; set; }
        public DeviceType DeviceType { get; set; }
    }
}

