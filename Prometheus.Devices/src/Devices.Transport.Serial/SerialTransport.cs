using System.IO.Ports;
using Devices.Transport.Abstractions;

namespace Devices.Transport.Serial;

/// <summary>
/// Реализация транспорта поверх последовательного порта (COM).
/// </summary>
public sealed class SerialTransport : ITransport
{
	private SerialPort? _port;

	/// <inheritdoc />
	public string Scheme => "serial";

	/// <summary>
	/// Открывает COM-порт (по умолчанию 115200 бод).
	/// </summary>
	public Task OpenAsync(EndpointAddress address, CancellationToken cancellationToken)
	{
		_port = new SerialPort(address.Path ?? "COM1", 115200);
		_port.Open();
		return Task.CompletedTask;
	}

	/// <inheritdoc />
	public Task<int> SendAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken)
	{
		if (_port is null) throw new InvalidOperationException("Transport not open");
		var bytes = payload.ToArray();
		_port.Write(bytes, 0, bytes.Length);
		return Task.FromResult(bytes.Length);
	}

	/// <inheritdoc />
	public Task<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
	{
		if (_port is null) throw new InvalidOperationException("Transport not open");
		var temp = new byte[buffer.Length];
		var read = _port.Read(temp, 0, temp.Length);
		temp.AsSpan(0, read).CopyTo(buffer.Span);
		return Task.FromResult(read);
	}

	/// <inheritdoc />
	public ValueTask DisposeAsync()
	{
		_port?.Dispose();
		_port = null;
		return ValueTask.CompletedTask;
	}
}

