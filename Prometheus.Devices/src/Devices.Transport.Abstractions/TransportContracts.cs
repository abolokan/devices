namespace Devices.Transport.Abstractions;

/// <summary>
/// Абстракция транспорта обмена данными с устройством (TCP, Serial, USB и т.д.).
/// </summary>
/// <remarks>
/// Транспорт скрывает детали соединения. Жизненный цикл: <see cref="OpenAsync"/> → <see cref="SendAsync"/>/<see cref="ReceiveAsync"/> → <see cref="IAsyncDisposable.DisposeAsync"/>.
/// </remarks>
public interface ITransport : IAsyncDisposable
{
	/// <summary>
	/// Схема транспорта, по которой его выбирает фабрика (например, "tcp", "serial").
	/// </summary>
    string Scheme { get; }

	/// <summary>
	/// Открывает соединение с устройством по заданному адресу.
	/// </summary>
	/// <param name="address">Адрес конечной точки (схема, хост, порт, путь/идентификатор).</param>
	/// <param name="cancellationToken">Токен отмены.</param>
	/// <example>
	/// Пример (TCP): <code>await transport.OpenAsync(new EndpointAddress("tcp", "192.168.0.10", 554, null), ct);</code>
	/// </example>
    Task OpenAsync(EndpointAddress address, CancellationToken cancellationToken);

	/// <summary>
	/// Отправляет бинарный буфер в устройство.
	/// </summary>
	/// <param name="payload">Данные для отправки.</param>
	/// <param name="cancellationToken">Токен отмены.</param>
	/// <returns>Количество отправленных байтов.</returns>
    Task<int> SendAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken);

	/// <summary>
	/// Получает бинарные данные от устройства в предоставленный буфер.
	/// </summary>
	/// <param name="buffer">Буфер для записи принятых данных.</param>
	/// <param name="cancellationToken">Токен отмены.</param>
	/// <returns>Количество прочитанных байтов.</returns>
    Task<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken);
}

/// <summary>
/// Адрес конечной точки для открытия соединения транспортом.
/// </summary>
/// <param name="Scheme">Схема (например, "tcp", "serial").</param>
/// <param name="Host">Хост/IP (для TCP), может быть null для Serial.</param>
/// <param name="Port">Порт (для TCP), может быть null.</param>
/// <param name="Path">Путь/идентификатор (например, COM-порт).</param>
public sealed record EndpointAddress(string Scheme, string? Host, int? Port, string? Path);

/// <summary>
/// Фабрика транспортов по схеме адреса.
/// </summary>
public interface ITransportFactory
{
	/// <summary>
	/// Создаёт реализацию <see cref="ITransport"/> по строковой схеме.
	/// </summary>
	/// <param name="scheme">Схема транспорта ("tcp", "serial").</param>
	/// <returns>Экземпляр транспорта.</returns>
    ITransport Create(string scheme);
}
