using Devices.Transport.Abstractions;

namespace Devices.Abstractions;

/// <summary>
/// Плагин для создания устройства из транспорта и объявления возможностей.
/// </summary>
public interface IDevicePlugin
{
	/// <summary>
	/// Уникальный идентификатор плагина (используется при разрешении).
	/// </summary>
	string PluginId { get; }

	/// <summary>
	/// Версия плагина (семантическая).
	/// </summary>
	Version PluginVersion { get; }

	/// <summary>
	/// Тип устройства, поддерживаемый плагином ("camera"/"printer"/"gate").
	/// </summary>
	string DeviceType { get; }

	/// <summary>
	/// Набор возможностей плагина (capabilities) в виде ключ-значение.
	/// </summary>
	IReadOnlyDictionary<string, string> Capabilities { get; }

	/// <summary>
	/// Создаёт устройство на базе предоставленного транспорта.
	/// </summary>
	/// <param name="transport">Открытый транспорт для обмена.</param>
	/// <param name="cancellationToken">Токен отмены.</param>
	/// <returns>Экземпляр устройства, реализующего нужный контракт (`ICamera`/`IPrinter`/`IGate`).</returns>
	Task<IDevice> CreateAsync(ITransport transport, CancellationToken cancellationToken);
}
