namespace Devices.Abstractions;

/// <summary>
/// Базовый контракт любого устройства (тип, производитель, модель, версия протокола).
/// </summary>
public interface IDevice
{
	/// <summary>
	/// Логический тип устройства (например, "camera", "printer", "gate").
	/// </summary>
	string DeviceType { get; }

	/// <summary>
	/// Производитель устройства.
	/// </summary>
	string Manufacturer { get; }

	/// <summary>
	/// Модель устройства у производителя.
	/// </summary>
	string Model { get; }

	/// <summary>
	/// Версия поддерживаемого протокола взаимодействия.
	/// </summary>
	Version ProtocolVersion { get; }
}
