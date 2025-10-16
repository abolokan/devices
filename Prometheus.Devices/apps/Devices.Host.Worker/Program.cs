using Devices.Host.Worker;
using Devices.Core;
using Devices.Transport.Abstractions;
using Devices.Transport.Tcp;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();
builder.Services.AddSingleton<ITransportFactory, SimpleTransportFactory>();
builder.Services.AddSingleton<IDevicePluginCatalog>(sp => new DirectoryPluginCatalog(Path.Combine(AppContext.BaseDirectory, "plugins")));
builder.Services.AddSingleton<DeviceManager>();

var host = builder.Build();
host.Run();

public sealed class SimpleTransportFactory : ITransportFactory
{
	/// <summary>
	/// Создаёт транспорт по строковой схеме. Поддерживает "tcp".
	/// </summary>
	/// <param name="scheme">Схема транспорта.</param>
	/// <returns>Экземпляр транспорта.</returns>
	public ITransport Create(string scheme) => scheme == "tcp" ? new TcpTransport() : throw new NotSupportedException(scheme);
}
