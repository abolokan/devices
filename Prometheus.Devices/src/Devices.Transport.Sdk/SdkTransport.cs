using System;
using System.Threading;
using System.Threading.Tasks;
using Devices.Transport.Abstractions;

namespace Devices.Transport.Sdk;

/// <summary>
/// Транспорт SDK - заглушка для работы с SDK/библиотеками, не требующими сетевого соединения.
/// </summary>
public sealed class SdkTransport : ITransport
{
	/// <inheritdoc />
	public string Scheme => "sdk";

	/// <summary>
	/// Для SDK не требуется реальное открытие соединения.
	/// </summary>
	public Task OpenAsync(EndpointAddress address, CancellationToken cancellationToken)
	{
		return Task.CompletedTask;
	}

	/// <inheritdoc />
	public Task<int> SendAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken)
	{
		return Task.FromResult(payload.Length);
	}

	/// <inheritdoc />
	public Task<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
	{
		return Task.FromResult(0);
	}

	/// <inheritdoc />
	public ValueTask DisposeAsync()
	{
		return ValueTask.CompletedTask;
	}
}

