namespace Prometheus.Devices.Core.Interfaces
{
    /// <summary>
    /// Базовый интерфейс для всех устройств
    /// </summary>
    public interface IDevice : IDisposable
    {
        /// <summary>
        /// Уникальный идентификатор устройства
        /// </summary>
        string DeviceId { get; }

        /// <summary>
        /// Название устройства
        /// </summary>
        string DeviceName { get; }

        /// <summary>
        /// Тип устройства
        /// </summary>
        DeviceType DeviceType { get; }

        /// <summary>
        /// Статус устройства
        /// </summary>
        DeviceStatus Status { get; }

        /// <summary>
        /// Подключение к устройству
        /// </summary>
        IConnection Connection { get; }

        /// <summary>
        /// Событие изменения статуса устройства
        /// </summary>
        event EventHandler<DeviceStatusChangedEventArgs> StatusChanged;

        /// <summary>
        /// Инициализировать устройство
        /// </summary>
        Task<bool> InitializeAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Подключиться к устройству
        /// </summary>
        Task<bool> ConnectAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Отключиться от устройства
        /// </summary>
        Task<bool> DisconnectAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Получить информацию об устройстве
        /// </summary>
        Task<DeviceInfo> GetDeviceInfoAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Сбросить устройство
        /// </summary>
        Task<bool> ResetAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Тип устройства
    /// </summary>
    public enum DeviceType
    {
        Unknown,
        Camera,
        Printer,
        Scanner,
        Sensor,
        Display,
        Other
    }

    /// <summary>
    /// Статус устройства
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
    /// Аргументы события изменения статуса устройства
    /// </summary>
    public class DeviceStatusChangedEventArgs : EventArgs
    {
        public DeviceStatus OldStatus { get; set; }
        public DeviceStatus NewStatus { get; set; }
        public string Message { get; set; }
    }

    /// <summary>
    /// Информация об устройстве
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

