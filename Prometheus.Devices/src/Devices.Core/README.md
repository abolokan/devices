# Devices.Core

- Назначение: минимальное ядро координации.
- Компоненты: `DeviceManager`, `DirectoryPluginCatalog`.
- Зависимости: `Devices.Abstractions`, `Devices.Transport.Abstractions`.

Поток вызовов:
1. `DeviceManager.ConnectAsync` создаёт `ITransport` через `ITransportFactory` и открывает соединение.
2. Разрешает плагин по `pluginId` через `IDevicePluginCatalog`.
3. Вызывает `plugin.CreateAsync(transport)` и возвращает унифицированное устройство (`ICamera`/`IPrinter`/`IGate`).

`DirectoryPluginCatalog` грузит *.dll из `plugins/` и регистрирует классы `IDevicePlugin`. Предупреждения trim допустимы для динамической загрузки.
