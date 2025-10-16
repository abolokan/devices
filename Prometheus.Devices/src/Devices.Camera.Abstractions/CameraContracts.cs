using Devices.Abstractions;

namespace Devices.Camera.Abstractions;

/// <summary>
/// Унифицированный контракт для камер наблюдения.
/// </summary>
public interface ICamera : IDevice
{
	/// <summary>
	/// Запускает получение кадров с указанием параметров.
	/// </summary>
	/// <param name="options">Параметры запуска (разрешение, FPS).</param>
	/// <param name="cancellationToken">Токен отмены.</param>
	/// <example>
	/// <code>
	/// await camera.StartAsync(new CameraStartOptions(1920, 1080, 25), ct);
	/// await foreach (var frame in camera.GetFramesAsync(ct)) { /* обработка */ }
	/// </code>
	/// </example>
	Task StartAsync(CameraStartOptions options, CancellationToken cancellationToken);

	/// <summary>
	/// Останавливает получение кадров.
	/// </summary>
	Task StopAsync(CancellationToken cancellationToken);

	/// <summary>
	/// Поток кадров в унифицированном формате.
	/// </summary>
	/// <param name="cancellationToken">Токен отмены.</param>
	/// <returns>Асинхронная последовательность кадров.</returns>
	IAsyncEnumerable<CameraFrame> GetFramesAsync(CancellationToken cancellationToken);
}
