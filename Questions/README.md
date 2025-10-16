## DeviceWrappers (C#) — унифицированные врапперы для камер, принтеров и др.

Архитектура предоставляет единые интерфейсы для работы с разными типами устройств и подключений: USB, TCP/IP, Serial. Подходит для промышленной интеграции и быстрой адаптации под конкретных вендоров.

### Ключевые возможности
- **Унифицированные интерфейсы**: `IConnection`, `IDevice`, `ICamera`, `IPrinter`, `IScanner`.
- **Подключения**: `TcpConnection`, `SerialConnection`, `UsbConnection` (заглушка для LibUsbDotNet/WinUSB), `NullConnection` для локальных устройств.
- **Промышленные принтеры**: `DriverPrinter` с ESC/POS (`EscPosDriver`, `BixolonBk331Driver`), конфигурация через JSON профили.
- **Офисные устройства (кроссплатформенные)**:
  - `OfficePrinter` — печать через Windows API (System.Drawing.Printing) или Linux CUPS
  - `OfficeScanner` — сканирование через Windows WIA или Linux SANE
  - Автоопределение платформы (`RuntimeInformation`)
- **Камеры**: `LocalCamera` (встроенная/USB через OpenCvSharp), `IpCamera`, `UsbCamera`.
- **Утилиты**: логирование, обработка ошибок, `RetryPolicy`, `DeviceFactory`, `DeviceManager`.

### Структура
```
Core/
  Interfaces/        # IConnection, IDevice, ICamera, IPrinter, IScanner
  Connections/       # TCP/Serial/USB-заглушка/NullConnection
  Devices/           # BaseDevice
  Drivers/           # IPrinterDriver
  Platform/          # IPlatformPrinter, IPlatformScanner
  Profiles/          # DeviceProfile, PrinterProfile
Devices/
  Camera/            # LocalCamera, IpCamera, UsbCamera, GenericCamera
  Printer/           # DriverPrinter, OfficePrinter, NetworkPrinter, SerialPrinter, UsbPrinter
  Scanner/           # OfficeScanner
Drivers/
  Printers/          # EscPosDriver, BixolonBk331Driver
Platform/
  Windows/           # WindowsPlatformPrinter, WindowsPlatformScanner
  Linux/             # LinuxPlatformPrinter, LinuxPlatformScanner
Utils/
  Logging/           # ConsoleLogger, FileLogger
  ErrorHandling/     # DeviceException, RetryPolicy
  DeviceFactory.cs   # Фабрика устройств + платформо-определение
  ProfileLoader.cs   # Загрузка профилей из JSON
Examples/
  Program.cs         # Тесты всех устройств
  printer.profile.json
```

### Быстрый старт

#### Локальная камера
```csharp
var cam = DeviceFactory.CreateLocalCamera(0, "Built-in Cam");
await cam.ConnectAsync();
await cam.InitializeAsync();
var frame = await cam.CaptureFrameAsync();
await cam.SaveFrameAsync(frame, "frame.jpg");
```

#### Промышленный принтер (ESC/POS)
```csharp
var profile = ProfileLoader.LoadPrinterProfile("printer.profile.json");
var driver = DeviceFactory.ResolvePrinterDriver(profile); // BixolonBk331Driver
var printer = DeviceFactory.CreateDriverPrinter(
    new TcpConnection("192.168.1.50", 9100), profile, driver);
await printer.ConnectAsync();
await printer.InitializeAsync();
await printer.PrintTextAsync("Привет, мир!"); // PC866 кодировка
```

#### Офисный принтер (Windows/Linux)
```csharp
// Получить список принтеров
var printers = await DeviceFactory.GetAvailableOfficePrintersAsync();

// Печать (Windows: System.Drawing.Printing, Linux: CUPS)
var printer = DeviceFactory.CreateOfficePrinter("Samsung_SCX-4200");
await printer.ConnectAsync();
await printer.InitializeAsync();
await printer.PrintTextAsync("Hello from DeviceWrappers!");
await printer.PrintFileAsync("document.pdf");
```

#### Сканер (Windows/Linux)
```csharp
// Получить список сканеров
var scanners = await DeviceFactory.GetAvailableScannersAsync();

// Сканирование (Windows: WIA, Linux: SANE)
var scanner = DeviceFactory.CreateOfficeScanner("samsung:usb:0x04e8:0x3413");
await scanner.ConnectAsync();
await scanner.InitializeAsync();
scanner.Settings.Resolution = 300;
scanner.Settings.ColorMode = ScanColorMode.Color;
var image = await scanner.ScanAsync();
await scanner.SaveImageAsync(image, "scan.jpg");
```

### USB-подключение
`UsbConnection` содержит каркас. Для боевого USB используйте:
- `LibUsbDotNet` (`DeviceWrappers.Core.Connections.UsbConnection`) — замените заглушки реальными вызовами.
- Либо WinUSB/Windows.Devices.Usb для UWP/Win.

### Расширение под вендора
1. Создайте наследника `BaseDevice` или `Generic*` класса.
2. Опишите протокол команд/ответов конкретного устройства.
3. Добавьте специфичные операции (например, калибровка камеры, резка чеков и т.д.).

### Обработка ошибок
- Бросайте `DeviceException` и его наследников.
- Для нестабильных операций используйте `RetryPolicy`.

### Логирование
- Подключите `ConsoleLogger` или `FileLogger` и прокиньте его в вашу бизнес-логику.

### Платформы

#### Windows
- **Офисные принтеры**: `System.Drawing.Printing` (требует `System.Drawing.Common`)
- **Сканеры**: WIA (Windows Image Acquisition, COM-библиотека)
- **Камеры**: OpenCvSharp4

#### Linux
- **Офисные принтеры**: CUPS (`lp`, `lpstat` команды)
- **Сканеры**: SANE (`scanimage` команда)
- **Камеры**: OpenCvSharp4

#### Установка
```bash
# Windows
dotnet add package System.Drawing.Common
dotnet add package OpenCvSharp4
dotnet add package OpenCvSharp4.runtime.win

# Linux (установите системные пакеты)
sudo apt-get install cups printer-driver-all
sudo apt-get install sane sane-utils
```

### Замечания
- **Промышленные принтеры** (ESC/POS): для Bixolon BK3-31 настройте `EscPosCodepage` в профиле (17 = PC866 кириллица).
- **USB-подключения**: `UsbConnection` — каркас. Для production используйте LibUsbDotNet/WinUSB.
- **WIA сканирование** (Windows): требует COM reference к `WIA` библиотеке.
- Код демонстрационный: команды `INIT`, `@PJL` — примеры. Замените на протокол производителя.


