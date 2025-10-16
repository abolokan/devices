using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Devices.Abstractions;
using Devices.Camera.Abstractions;
using Devices.Transport.Abstractions;
using OpenCvSharp;

namespace Devices.Camera.LogitechC270;

/// <summary>
/// Плагин камеры Logitech C270 HD WebCam на базе OpenCvSharp.
/// </summary>
[DevicePlugin("logitech.camera.c270", "camera", "Logitech", "C270")]
public sealed class LogitechC270CameraPlugin : IDevicePlugin
{
	/// <inheritdoc />
	public string PluginId => "logitech.camera.c270";
	/// <inheritdoc />
	public Version PluginVersion => new(1, 0, 0);
	/// <inheritdoc />
	public string DeviceType => "camera";
	/// <inheritdoc />
	public IReadOnlyDictionary<string, string> Capabilities => new Dictionary<string, string>
	{
		["video.stream"] = "mjpeg",
		["transport"] = "sdk"
	};

	/// <inheritdoc />
	public Task<IDevice> CreateAsync(ITransport transport, CancellationToken cancellationToken)
	{
		return Task.FromResult<IDevice>(new LogitechC270Camera());
	}
}

/// <summary>
/// Реализация ICamera для Logitech C270 через OpenCvSharp.
/// </summary>
internal sealed class LogitechC270Camera : ICamera
{
	private VideoCapture? _capture;
	private bool _isRunning;

	/// <inheritdoc />
	public string DeviceType => "camera";
	/// <inheritdoc />
	public string Manufacturer => "Logitech";
	/// <inheritdoc />
	public string Model => "C270";
	/// <inheritdoc />
	public Version ProtocolVersion => new(1, 0);

	/// <inheritdoc />
	public Task StartAsync(CameraStartOptions options, CancellationToken cancellationToken)
	{
		_capture = new VideoCapture(0); // 0 = первая камера
		_capture.Set(VideoCaptureProperties.FrameWidth, options.Width);
		_capture.Set(VideoCaptureProperties.FrameHeight, options.Height);
		_capture.Set(VideoCaptureProperties.Fps, options.Fps);
		_isRunning = true;
		return Task.CompletedTask;
	}

	/// <inheritdoc />
	public Task StopAsync(CancellationToken cancellationToken)
	{
		_isRunning = false;
		_capture?.Release();
		_capture?.Dispose();
		_capture = null;
		return Task.CompletedTask;
	}

	/// <inheritdoc />
	public async IAsyncEnumerable<CameraFrame> GetFramesAsync([EnumeratorCancellation] CancellationToken cancellationToken)
	{
		if (_capture == null || !_isRunning) yield break;

		using var frame = new Mat();
		while (_isRunning && !cancellationToken.IsCancellationRequested)
		{
			if (_capture.Read(frame) && !frame.Empty())
			{
				var bytes = frame.ToBytes(".jpg");
				yield return new CameraFrame(DateTimeOffset.UtcNow, bytes, "jpeg");
			}
			await Task.Delay(33, cancellationToken); // ~30 FPS
		}
	}
}

