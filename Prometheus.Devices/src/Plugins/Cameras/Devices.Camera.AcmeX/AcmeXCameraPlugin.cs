using System.Runtime.CompilerServices;
using Devices.Abstractions;
using Devices.Camera.Abstractions;
using Devices.Transport.Abstractions;

namespace Devices.Camera.AcmeX;

/// <summary>
/// Плагин камеры Acme X. Создаёт устройство камеры на основе предоставленного транспорта.
/// </summary>
/// <remarks>
/// Пример использования: <code>var cam = await plugin.CreateAsync(transport, ct) as ICamera;</code>
/// </remarks>
[DevicePlugin("acme.camera.x", "camera", "Acme", "X")]
public sealed class AcmeXCameraPlugin : IDevicePlugin
{
	/// <summary>Уникальный идентификатор плагина.</summary>
	public string PluginId => "acme.camera.x";
	/// <summary>Версия плагина.</summary>
	public Version PluginVersion => new(1, 0, 0);
	/// <summary>Поддерживаемый тип устройства.</summary>
	public string DeviceType => "camera";
	/// <summary>Набор возможностей (например, поддерживаемый кодек/транспорты).</summary>
	public IReadOnlyDictionary<string, string> Capabilities => new Dictionary<string, string>
	{
		["video.stream"] = "h264",
		["transport"] = "tcp"
	};

	/// <summary>
	/// Создаёт и инициализирует устройство камеры производителя Acme (модель X).
	/// </summary>
	/// <param name="transport">Открытый транспорт (tcp/serial).</param>
	/// <param name="cancellationToken">Токен отмены.</param>
	/// <returns>Экземпляр <see cref="ICamera"/>.</returns>
	/// <example>
	/// <code>
	/// var plugin = new AcmeXCameraPlugin();
	/// var camera = (ICamera) await plugin.CreateAsync(transport, ct);
	/// await camera.StartAsync(new CameraStartOptions(1280, 720, 30), ct);
	/// await foreach (var frame in camera.GetFramesAsync(ct)) { /* обработка */ }
	/// </code>
	/// </example>
	public async Task<IDevice> CreateAsync(ITransport transport, CancellationToken cancellationToken)
	{
		await transport.SendAsync(new byte[]{0x01}, cancellationToken);
		return new AcmeXCamera(transport);
	}
}

/// <summary>
/// Адаптер конкретной камеры Acme X к унифицированному контракту <see cref="ICamera"/>.
/// </summary>
internal sealed class AcmeXCamera : ICamera
{
	private readonly ITransport _transport;

	/// <summary>Создаёт адаптер с использованием транспорта.</summary>
	public AcmeXCamera(ITransport transport)
	{
		_transport = transport;
	}

	/// <inheritdoc />
	public string DeviceType => "camera";
	/// <inheritdoc />
	public string Manufacturer => "Acme";
	/// <inheritdoc />
	public string Model => "X";
	/// <inheritdoc />
	public Version ProtocolVersion => new(1, 0);

	/// <inheritdoc />
	public Task StartAsync(CameraStartOptions options, CancellationToken cancellationToken) => Task.CompletedTask;
	/// <inheritdoc />
	public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

	/// <inheritdoc />
	public async IAsyncEnumerable<CameraFrame> GetFramesAsync([EnumeratorCancellation] CancellationToken cancellationToken)
	{
		// Демонстрационный источник кадров
		for (var i = 0; i < 3; i++)
		{
			await Task.Delay(10, cancellationToken);
			yield return new CameraFrame(DateTimeOffset.UtcNow, new byte[] { 0x00, 0x01 }, "h264");
		}
	}
}
