namespace Devices.Abstractions;

/// <summary>
/// Атрибут-метаданные для декларативного описания плагина (id, тип, производитель, модель).
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class DevicePluginAttribute : Attribute
{
	/// <summary>
	/// Создаёт атрибут плагина.
	/// </summary>
	/// <param name="pluginId">Уникальный id плагина.</param>
	/// <param name="deviceType">Тип устройства ("camera"/"printer"/"gate").</param>
	/// <param name="manufacturer">Производитель.</param>
	/// <param name="model">Модель.</param>
	public DevicePluginAttribute(string pluginId, string deviceType, string manufacturer, string model)
	{
		PluginId = pluginId;
		DeviceType = deviceType;
		Manufacturer = manufacturer;
		Model = model;
	}

	/// <summary>Уникальный id плагина.</summary>
	public string PluginId { get; }
	/// <summary>Тип устройства.</summary>
	public string DeviceType { get; }
	/// <summary>Производитель.</summary>
	public string Manufacturer { get; }
	/// <summary>Модель.</summary>
	public string Model { get; }
}
