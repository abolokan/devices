# Быстрый старт: съёмка с Logitech C270

## Предварительные требования
- Windows (OpenCvSharp включает нативные библиотеки для Windows)
- Подключённая Logitech C270 HD WebCam (или любая USB-камера)
- .NET 9.0 SDK

## Шаги запуска

### 1. Собрать решение
```powershell
cd Prometheus.Devices
dotnet build -c Release
```

### 2. Скопировать плагин камеры и зависимости
```powershell
# Копируем DLL плагина и OpenCvSharp в plugins
xcopy /Y src\SDKs\Logitech\Devices.Camera.LogitechC270\bin\Release\net9.0\*.dll apps\Devices.Host.Cli\bin\Release\net9.0\win-x64\plugins\

# Копируем OpenCvSharp в корень (для резолвинга зависимостей)
xcopy /Y src\SDKs\Logitech\Devices.Camera.LogitechC270\bin\Release\net9.0\OpenCvSharp.dll apps\Devices.Host.Cli\bin\Release\net9.0\win-x64\

# Копируем нативные библиотеки OpenCvSharp
xcopy /Y /S src\SDKs\Logitech\Devices.Camera.LogitechC270\bin\Release\net9.0\runtimes apps\Devices.Host.Cli\bin\Release\net9.0\win-x64\runtimes\
```

### 3. Запустить CLI
```powershell
cd apps\Devices.Host.Cli\bin\Release\net9.0\win-x64
.\Devices.Host.Cli.exe
```

### 4. Команды
- **Снять одно фото**: `snapshot logitech.camera.c270`
- **Записать видео (10 секунд)**: `record logitech.camera.c270 10`

## Результаты
- Фото сохраняется как `snapshot_<дата_время>.jpg` в текущей папке.
- Видео сохраняется как последовательность кадров в папке `video_frames/`.

## Примечание
- Плагин использует OpenCvSharp для захвата с камеры (индекс 0 = первая камера).
- Если у вас несколько камер, код берёт первую обнаруженную.
- Разрешение по умолчанию: 1280x720 @30fps.

## Проверка подключения камеры
Убедитесь, что камера видна в диспетчере устройств Windows (раздел "Камеры").

