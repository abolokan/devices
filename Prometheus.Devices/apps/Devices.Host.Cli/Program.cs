using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Devices.Core;
using Devices.Camera.Abstractions;
using Devices.Transport.Abstractions;
using Devices.Transport.Tcp;
using Devices.Transport.Sdk;

var transportFactory = new SimpleTransportFactory();
var pluginsPath = Path.Combine(AppContext.BaseDirectory, "plugins");
var pluginCatalog = new DirectoryPluginCatalog(pluginsPath);
var manager = new DeviceManager(transportFactory, pluginCatalog);

Console.WriteLine($"Devices.Host.Cli booted. Plugins path: {pluginsPath}");
Console.WriteLine("Команды: snapshot <pluginId> | record <pluginId> <секунд>");

var input = Console.ReadLine();
if (!string.IsNullOrWhiteSpace(input))
{
	var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
	var command = parts[0].ToLower();

	if (command == "snapshot" && parts.Length >= 2)
	{
		await CaptureSnapshot(manager, parts[1], CancellationToken.None);
	}
	else if (command == "record" && parts.Length >= 3 && int.TryParse(parts[2], out var seconds))
	{
		await RecordVideo(manager, parts[1], seconds, CancellationToken.None);
	}
}

Console.WriteLine("Завершено.");

static async Task CaptureSnapshot(DeviceManager manager, string pluginId, CancellationToken ct)
{
	var address = new EndpointAddress("sdk", null, null, null);
	var camera = await manager.ConnectAsync<ICamera>(address, pluginId, ct);
	await camera.StartAsync(new CameraStartOptions(1280, 720, 30), ct);

	await foreach (var frame in camera.GetFramesAsync(ct))
	{
		var fileName = $"snapshot_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
		await File.WriteAllBytesAsync(fileName, frame.Data.ToArray(), ct);
		Console.WriteLine($"Сохранён кадр: {fileName}");
		break;
	}

	await camera.StopAsync(ct);
}

static async Task RecordVideo(DeviceManager manager, string pluginId, int seconds, CancellationToken ct)
{
	var address = new EndpointAddress("sdk", null, null, null);
	var camera = await manager.ConnectAsync<ICamera>(address, pluginId, ct);
	await camera.StartAsync(new CameraStartOptions(1280, 720, 30), ct);

	var outputDir = "video_frames";
	Directory.CreateDirectory(outputDir);
	var frameCount = 0;

	using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(seconds));
	var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, cts.Token);

	await foreach (var frame in camera.GetFramesAsync(combinedCts.Token))
	{
		var fileName = Path.Combine(outputDir, $"frame_{frameCount:D5}.jpg");
		await File.WriteAllBytesAsync(fileName, frame.Data.ToArray(), combinedCts.Token);
		frameCount++;
	}

	await camera.StopAsync(ct);
	Console.WriteLine($"Записано {frameCount} кадров в {outputDir}/");
}

/// <summary>
/// Простая фабрика транспортов для демо. Поддерживает схему "tcp" и "sdk".
/// </summary>
public sealed class SimpleTransportFactory : ITransportFactory
{
	/// <summary>
	/// Создаёт транспорт по строковой схеме.
	/// </summary>
	/// <param name="scheme">Схема (например, "tcp", "sdk").</param>
	/// <returns>Экземпляр транспорта.</returns>
	public ITransport Create(string scheme) => scheme switch
	{
		"tcp" => new TcpTransport(),
		"sdk" => new SdkTransport(),
		_ => throw new NotSupportedException(scheme)
	};
}
