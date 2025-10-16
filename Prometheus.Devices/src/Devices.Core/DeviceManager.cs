using Devices.Abstractions;
using Devices.Transport.Abstractions;

namespace Devices.Core;

/// <summary>
/// Унифицированная точка входа для подключения к устройствам через транспорты и плагины.
/// </summary>
public sealed class DeviceManager
{
	private readonly ITransportFactory _transportFactory;
	private readonly IDevicePluginCatalog _pluginCatalog;

	public DeviceManager(ITransportFactory transportFactory, IDevicePluginCatalog pluginCatalog)
	{
		_transportFactory = transportFactory;
		_pluginCatalog = pluginCatalog;
	}

	/// <summary>
	/// Подключается к устройству, создавая транспорт и инициализируя плагин.
	/// </summary>
	/// <typeparam name="TDevice">Ожидаемый контракт устройства (например, ICamera).</typeparam>
	/// <param name="address">Адрес соединения.</param>
	/// <param name="pluginId">Идентификатор плагина.</param>
	/// <param name="cancellationToken">Токен отмены.</param>
	/// <returns>Экземпляр устройства, реализующий контракт <typeparamref name="TDevice"/>.</returns>
	public async Task<TDevice> ConnectAsync<TDevice>(EndpointAddress address, string pluginId, CancellationToken cancellationToken)
		where TDevice : class, IDevice
	{
		var transport = _transportFactory.Create(address.Scheme);
		await transport.OpenAsync(address, cancellationToken);
		var plugin = _pluginCatalog.Resolve(pluginId);
		var device = await plugin.CreateAsync(transport, cancellationToken);
		return (TDevice)device;
	}
}
