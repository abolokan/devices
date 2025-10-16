# Devices.Camera.AcmeX

- Назначение: пример плагина камеры Acme X.
- Точка входа: `AcmeXCameraPlugin` (реализует `IDevicePlugin`).
- Возвращаемый тип устройства: `ICamera` (через внутренний адаптер `AcmeXCamera`).

Зависимости: `Devices.Camera.Abstractions`, `Devices.Abstractions`, `Devices.Transport.Abstractions`. Загружается динамически из `plugins/`.
