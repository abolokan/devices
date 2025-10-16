# Devices.Host.Cli

- Назначение: консольный хост для запуска устройств и тестов.
- Что делает: настраивает `ITransportFactory`, создаёт `DirectoryPluginCatalog` (`plugins/`), инициализирует `DeviceManager`.
- Запуск публикации: `pwsh ./publish.ps1 -app cli -plugins Devices.Camera.AcmeX`.

Плагины копируются в `publish/plugins` и подхватываются динамически при старте.
