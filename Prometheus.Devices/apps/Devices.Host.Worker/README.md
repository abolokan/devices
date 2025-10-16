# Devices.Host.Worker

- Назначение: служба (Worker Service) для продакшн.
- Что делает: регистрирует `DeviceManager`, `DirectoryPluginCatalog` (путь `plugins/`), `ITransportFactory` и запускает фоновые задачи в `Worker`.
- Публикация: `pwsh ./publish.ps1 -app worker -plugins <список плагинов>`.

Плагины кладутся в `publish/plugins` и загружаются динамически.
