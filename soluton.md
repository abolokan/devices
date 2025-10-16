## Модульная архитектура для .NET устройств и избирательной публикации

Цель: гибкая и поддерживаемая архитектура, позволяющая подключать/обновлять конкретные устройства (камеры, принтеры, шлагбаумы и т.д.) и публиковать только необходимые сборки.

### Ключевые принципы
- SRP/ISP: узкие интерфейсы под тип устройства и под транспорт.
- Плагины: каждый конкретный производитель/версия — отдельная сборка.
- Transport-agnostic: ядро понимает USB/TCP/Serial через абстракции транспорта.
- Версионирование протокола: capability-модель и семантические версии в метаданных плагина.
- Избирательная поставка: trim-friendly, self-contained/portable, RID-специфичные публикации.

### Структура решений (папки/проекты)

```
Edge-Devices.sln
  src/
    Devices.Abstractions/                # Общие интерфейсы устройств и capability-модель
    Devices.Core/                        # Минималистичное ядро: диспетчер, фабрики, транспортные адаптеры
    Devices.Transport.Abstractions/      # ITransport, адреса, конвейер сообщений
    Devices.Transport.Tcp/               # Реализация TCP
    Devices.Transport.Usb/               # Реализация USB (через WinUSB/libusb обертки)
    Devices.Transport.Serial/            # Реализация Serial
    Devices.Camera.Abstractions/         # Узкие интерфейсы для камер (общий формат событий/кадра)
    Devices.Printer.Abstractions/        # Узкие интерфейсы для принтеров
    Devices.Gate.Abstractions/           # Узкие интерфейсы для шлагбаумов
    Plugins/
      Cameras/
        Devices.Camera.AcmeX/            # Плагин камеры производителя Acme, модель X
        Devices.Camera.ContosoY/         # Плагин камеры производителя Contoso, модель Y
      Printers/
        Devices.Printer.ZebraZ/          # Плагин принтера
      Gates/
        Devices.Gate.BarrierV1/          # Плагин шлагбаума
    Apps/
      Devices.Host.Cli/                  # Хост-приложение (консоль/служба) с DI и загрузкой плагинов
      Devices.Host.Worker/               # Worker Service (IHostedService) для продакшн
  docs/
    soluton.md
```

### Минималистичное ядро

Цель ядра — унификация транспорта и обмена сообщениями, форматирование данных до общего контракта. Ядро не знает конкретных производителей; оно загружает плагины, проверяет их capability и маршрутизирует сообщения.

Интерфейсы уровней:

```csharp
// Devices.Transport.Abstractions
public interface ITransport : IAsyncDisposable
{
    string Scheme { get; }                 // "tcp", "usb", "serial"
    Task OpenAsync(EndpointAddress address, CancellationToken ct);
    Task<int> SendAsync(ReadOnlyMemory<byte> payload, CancellationToken ct);
    Task<int> ReceiveAsync(Memory<byte> buffer, CancellationToken ct);
}

public sealed record EndpointAddress(string Scheme, string Host, int? Port, string? Path);

// Devices.Abstractions
public interface IDevice
{
    string DeviceType { get; }             // "camera", "printer", "gate"
    string Manufacturer { get; }
    string Model { get; }
    Version ProtocolVersion { get; }
}

public interface IDevicePlugin
{
    string PluginId { get; }
    Version PluginVersion { get; }
    string DeviceType { get; }
    IReadOnlyDictionary<string, string> Capabilities { get; } // например {"video.stream":"h264"}
    Task<IDevice> CreateAsync(ITransport transport, CancellationToken ct);
}

// Специализация для камер
public interface ICamera : IDevice
{
    Task StartAsync(CameraStartOptions options, CancellationToken ct);
    Task StopAsync(CancellationToken ct);
    IAsyncEnumerable<CameraFrame> GetFramesAsync(CancellationToken ct);
}

public sealed record CameraStartOptions(int Width, int Height, int Fps);
public sealed record CameraFrame(DateTimeOffset Timestamp, ReadOnlyMemory<byte> Data, string Format);
```

Ядро (Devices.Core) предоставляет:
- `DevicePluginLoader`: загрузка плагинов через `AssemblyLoadContext`/MEF/рефлексию по контракту `IDevicePlugin`.
- `TransportFactory`: маппинг схемы адреса на конкретную реализацию `ITransport`.
- `DeviceManager`: координация создания устройств, кэш жизненного цикла, маршрутизация команд.

```csharp
public interface ITransportFactory
{
    ITransport Create(string scheme);
}

public sealed class DeviceManager
{
    private readonly ITransportFactory _transportFactory;
    private readonly IDevicePluginCatalog _pluginCatalog;

    public DeviceManager(ITransportFactory transportFactory, IDevicePluginCatalog pluginCatalog)
    {
        _transportFactory = transportFactory;
        _pluginCatalog = pluginCatalog;
    }

    public async Task<TDevice> ConnectAsync<TDevice>(EndpointAddress address, string pluginId, CancellationToken ct)
        where TDevice : class, IDevice
    {
        var transport = _transportFactory.Create(address.Scheme);
        await transport.OpenAsync(address, ct);
        var plugin = _pluginCatalog.Resolve(pluginId);
        var device = await plugin.CreateAsync(transport, ct);
        return (TDevice)device;
    }
}
```

### Плагин-ориентированная модель

Каждый плагин — отдельная сборка. Он:
- Помечен атрибутами (метаданные) для быстрой фильтрации по типу/вендору/модели/версиям.
- Реализует `IDevicePlugin` и конкретные интерфейсы (`ICamera`, `IPrinter`, `IGate`).
- Не зависит от других плагинов; зависит только от `Devices.Abstractions`, `Devices.Transport.Abstractions`.

```csharp
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class DevicePluginAttribute : Attribute
{
    public DevicePluginAttribute(string pluginId, string deviceType, string manufacturer, string model) { /*...*/ }
    public string PluginId { get; }
    public string DeviceType { get; }
    public string Manufacturer { get; }
    public string Model { get; }
}

[DevicePlugin("acme.camera.x", "camera", "Acme", "X")]
public sealed class AcmeXCameraPlugin : IDevicePlugin
{
    public string PluginId => "acme.camera.x";
    public Version PluginVersion => new(1,0,0);
    public string DeviceType => "camera";
    public IReadOnlyDictionary<string,string> Capabilities => new Dictionary<string,string>
    {
        ["video.stream"] = "h264",
        ["transport"] = "tcp|usb"
    };

    public async Task<IDevice> CreateAsync(ITransport transport, CancellationToken ct)
    {
        // Обертка над транспортом + протокольный адаптер в общий формат ICamera
        var camera = new AcmeXCamera(transport);
        await camera.HandshakeAsync(ct);
        return camera;
    }
}
```

### Минимальная библиотека для общения с устройствами

Задачи:
- инкапсулировать транспорт (USB/TCP/Serial) за `ITransport`
- предоставить общий формат данных и событий для типа устройства
- обеспечить конвейер: encode/decode, retry, timeout, logging hooks

Компоненты ядра:
- `MessageEncoder`/`MessageDecoder` для двоичных протоколов
- `PipelineBehavior` (политики повторов/таймаутов)
- `DeviceSerializer` для DTO общего формата

```csharp
public interface IMessageCodec
{
    ReadOnlyMemory<byte> Encode<T>(T message);
    T Decode<T>(ReadOnlyMemory<byte> payload);
}

public sealed class DeviceClient
{
    private readonly ITransport _transport;
    private readonly IMessageCodec _codec;

    public DeviceClient(ITransport transport, IMessageCodec codec)
    {
        _transport = transport;
        _codec = codec;
    }

    public async Task<TResponse> SendAsync<TRequest, TResponse>(TRequest request, CancellationToken ct)
    {
        var bytes = _codec.Encode(request);
        await _transport.SendAsync(bytes, ct);
        var buffer = new byte[64 * 1024];
        var read = await _transport.ReceiveAsync(buffer, ct);
        return _codec.Decode<TResponse>(buffer.AsMemory(0, read));
    }
}
```

### Загрузка и избирательная публикация

Опции загрузки плагинов:
- Встроенная: ссылки на нужные плагины в хосте (+ trimming лишнего)
- Динамическая: сканирование папки `plugins/` и `AssemblyLoadContext` с изоляцией

Рекомендуемая для поставки: динамическая, чтобы просто класть нужные DLL.

```csharp
public interface IDevicePluginCatalog
{
    IDevicePlugin Resolve(string pluginId);
    IEnumerable<IDevicePlugin> FindByDeviceType(string deviceType);
}

public sealed class DirectoryPluginCatalog : IDevicePluginCatalog
{
    public DirectoryPluginCatalog(string directory) { /*...*/ }
    public IDevicePlugin Resolve(string pluginId) { /* load by metadata */ }
    public IEnumerable<IDevicePlugin> FindByDeviceType(string deviceType) { /*...*/ }
}
```

Публикация (избирательные сборки):
- Каждое семейство плагинов — отдельный `csproj`, публикуется отдельно.
- Хост не ссылается на плагины напрямую (или использует `AssemblyMetadataReference` только для compile-time контрактов).
- В установку копируются только нужные плагины и нужные транспорты (например, `Devices.Transport.Tcp.dll`).

`Devices.Host.Worker` пример `csproj` (фрагменты):

```xml
<Project Sdk="Microsoft.NET.Sdk.Worker">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <PublishSingleFile>true</PublishSingleFile>
    <SelfContained>true</SelfContained>
    <PublishTrimmed>true</PublishTrimmed>
    <InvariantGlobalization>true</InvariantGlobalization>
    <EnableTrimAnalyzer>true</EnableTrimAnalyzer>
  </PropertyGroup>

  <!-- Ссылки только на абстракции и ядро -->
  <ItemGroup>
    <ProjectReference Include="..\..\Devices.Abstractions\Devices.Abstractions.csproj" />
    <ProjectReference Include="..\..\Devices.Core\Devices.Core.csproj" />
    <ProjectReference Include="..\..\Devices.Transport.Abstractions\Devices.Transport.Abstractions.csproj" />
    <ProjectReference Include="..\..\Devices.Transport.Tcp\Devices.Transport.Tcp.csproj" />
  </ItemGroup>

  <!-- Папка плагинов копируется как есть -->
  <ItemGroup>
    <None Include="plugins\**\*.dll" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>
</Project>
```

CLI публикация выборочно:

```bash
dotnet publish src/Apps/Devices.Host.Worker -c Release -r win-x64 -p:SelfContained=true
# Затем положить в output только нужные плагины:
#   plugins/Cameras/Devices.Camera.AcmeX.dll
#   plugins/Printers/Devices.Printer.ZebraZ.dll
#   транспорты: Devices.Transport.Tcp.dll, Devices.Transport.Usb.dll (по необходимости)
```

### Версионирование и совместимость
- Семантические версии у плагинов: `PluginVersion` и `ProtocolVersion` устройства.
- Capability-модель вместо жёстких проверок: ключ-значение (например поддержка H264, MJPEG, управление PTZ).
- Контракты абстракций стабильны; изменения через расширения интерфейсов и новые DTO.

### Обновления и замена сборок
- Плагин загружается в изолированный `AssemblyLoadContext`; поддерживается hot-swap (остановить устройство -> выгрузить -> заменить DLL -> перезагрузить).
- Транспорты версионируются отдельно: можно обновить реализацию TCP не трогая плагины.

### Тестирование
- Контрактные тесты для каждого плагина против симулятора (виртуального девайса).
- Интеграционные тесты хоста с набором плагинов.

### Пример использования в хосте

```csharp
var services = new ServiceCollection()
    .AddSingleton<ITransportFactory, TransportFactory>()
    .AddSingleton<IDevicePluginCatalog>(sp => new DirectoryPluginCatalog(Path.Combine(AppContext.BaseDirectory, "plugins")))
    .AddSingleton<DeviceManager>()
    .BuildServiceProvider();

var manager = services.GetRequiredService<DeviceManager>();
var address = new EndpointAddress("tcp", "192.168.1.10", 554, null);
var camera = await manager.ConnectAsync<ICamera>(address, pluginId: "acme.camera.x", ct);
await foreach (var frame in camera.GetFramesAsync(ct))
{
    // Обработка стандартного формата кадра
}
```

### Итого
- Абстракции разделяют тип устройства и транспорт.
- Плагины изолированы и поставляются выборочно.
- Ядро минимально и отвечает за загрузку, маршрутизацию и унификацию форматов.
- Публикация — self-contained + trimming; поставляются только нужные плагины и транспорты.


