namespace Devices.Camera.Abstractions;

/// <summary>
/// Кадр видеопотока (метка времени, данные, формат кодека).
/// </summary>
public sealed record CameraFrame(DateTimeOffset Timestamp, ReadOnlyMemory<byte> Data, string Format);
