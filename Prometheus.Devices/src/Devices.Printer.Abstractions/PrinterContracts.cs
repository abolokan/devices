using Devices.Abstractions;

namespace Devices.Printer.Abstractions;

/// <summary>
/// Унифицированный контракт для принтеров.
/// </summary>
public interface IPrinter : IDevice
{
	/// <summary>
	/// Печатает переданные бинарные данные на устройстве.
	/// </summary>
	/// <param name="data">Сырые данные печати (например, ZPL/ESC/POS).</param>
	/// <param name="cancellationToken">Токен отмены.</param>
	Task PrintAsync(byte[] data, CancellationToken cancellationToken);
}
