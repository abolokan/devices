namespace Devices.Camera.Abstractions;

/// <summary>
/// Параметры запуска видеопотока камеры.
/// </summary>
public sealed record CameraStartOptions(int Width, int Height, int Fps);
