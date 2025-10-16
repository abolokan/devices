# Devices.Abstractions

- Назначение: общие контракты устройств и плагинов.
- Ключевые интерфейсы: `IDevice`, `IDevicePlugin`, атрибут `DevicePluginAttribute`.
- Зависимости: `Devices.Transport.Abstractions` (для `ITransport` в `IDevicePlugin.CreateAsync`).
- Используется: ядром (`Devices.Core`), типовыми абстракциями, транспортами и всеми плагинами.

Ключевая ценность: стабильные контракты и capability-модель (ключ-значение) для расширяемости без ломающих изменений.
