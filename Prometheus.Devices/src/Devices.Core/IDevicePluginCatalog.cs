using Devices.Abstractions;

namespace Devices.Core;

/// <summary>
/// Каталог плагинов устройств. Позволяет разрешать плагин по его идентификатору.
/// </summary>
public interface IDevicePluginCatalog
{
	/// <summary>
	/// Возвращает плагин по идентификатору, либо бросает исключение при отсутствии.
	/// </summary>
	/// <param name="pluginId">Идентификатор плагина.</param>
	IDevicePlugin Resolve(string pluginId);
}
