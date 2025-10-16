using Devices.Abstractions;

namespace Devices.Gate.Abstractions;

/// <summary>
/// Унифицированный контракт для ворот/шлагбаумов.
/// </summary>
public interface IGate : IDevice
{
	/// <summary>
	/// Открывает ворота/шлагбаум.
	/// </summary>
	Task OpenAsync(CancellationToken cancellationToken);
	/// <summary>
	/// Закрывает ворота/шлагбаум.
	/// </summary>
	Task CloseAsync(CancellationToken cancellationToken);
}
