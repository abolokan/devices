namespace Prometheus.Devices.Core.Interfaces
{
    /// <summary>
    /// Базовый интерфейс для всех типов подключений к устройствам
    /// </summary>
    public interface IConnection : IDisposable
    {
        /// <summary>
        /// Статус подключения
        /// </summary>
        ConnectionStatus Status { get; }

        /// <summary>
        /// Информация о подключении
        /// </summary>
        string ConnectionInfo { get; }

        /// <summary>
        /// Событие изменения статуса подключения
        /// </summary>
        event EventHandler<ConnectionStatusChangedEventArgs> StatusChanged;

        /// <summary>
        /// Открыть подключение
        /// </summary>
        Task OpenAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Закрыть подключение
        /// </summary>
        Task CloseAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Отправить данные
        /// </summary>
        Task<int> SendAsync(byte[] data, CancellationToken cancellationToken = default);

        /// <summary>
        /// Получить данные
        /// </summary>
        Task<byte[]> ReceiveAsync(int bufferSize = 4096, CancellationToken cancellationToken = default);

        /// <summary>
        /// Проверить доступность подключения
        /// </summary>
        Task<bool> PingAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Статус подключения
    /// </summary>
    public enum ConnectionStatus
    {
        Disconnected,
        Connecting,
        Connected,
        Disconnecting,
        Error
    }

    /// <summary>
    /// Аргументы события изменения статуса
    /// </summary>
    public class ConnectionStatusChangedEventArgs : EventArgs
    {
        public ConnectionStatus OldStatus { get; set; }
        public ConnectionStatus NewStatus { get; set; }
        public string Message { get; set; }
        public Exception Error { get; set; }
    }
}

