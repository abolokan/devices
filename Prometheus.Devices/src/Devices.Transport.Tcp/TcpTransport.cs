using System.Net.Sockets;
using Devices.Transport.Abstractions;

namespace Devices.Transport.Tcp;

/// <summary>
/// Реализация транспорта поверх TCP-клиента.
/// </summary>
public sealed class TcpTransport : ITransport
{
	private TcpClient? _client;

	/// <inheritdoc />
	public string Scheme => "tcp";

	/// <summary>
	/// Устанавливает TCP-соединение по адресу (host:port).
	/// </summary>
	public async Task OpenAsync(EndpointAddress address, CancellationToken cancellationToken)
	{
		_client = new TcpClient();
		await _client.ConnectAsync(address.Host!, address.Port!.Value, cancellationToken);
	}

	/// <inheritdoc />
	public async Task<int> SendAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken)
	{
		if (_client is null) throw new InvalidOperationException("Transport not open");
		await _client.GetStream().WriteAsync(payload, cancellationToken);
		return payload.Length;
	}

	/// <inheritdoc />
	public async Task<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
	{
		if (_client is null) throw new InvalidOperationException("Transport not open");
		return await _client.GetStream().ReadAsync(buffer, cancellationToken);
	}

	/// <inheritdoc />
	public ValueTask DisposeAsync()
	{
		_client?.Dispose();
		_client = null;
		return ValueTask.CompletedTask;
	}
}
