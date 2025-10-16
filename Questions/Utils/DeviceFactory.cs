using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using DeviceWrappers.Core.Interfaces;
using DeviceWrappers.Core.Platform;
using DeviceWrappers.Devices.Camera;
using DeviceWrappers.Devices.Printer;
using DeviceWrappers.Devices.Scanner;
using DeviceWrappers.Core.Drivers;
using DeviceWrappers.Core.Profiles;
using DeviceWrappers.Drivers.Printers;
using DeviceWrappers.Platform.Windows;
using DeviceWrappers.Platform.Linux;
using OpenCvSharp;

namespace DeviceWrappers.Utils
{
    /// <summary>
    /// Фабрика для создания устройств различных типов
    /// </summary>
    public static class DeviceFactory
    {
        /// <summary>
        /// Создать локальную камеру по индексу (0 — как правило, встроенная)
        /// </summary>
        public static ICamera CreateLocalCamera(int index = 0, string name = null)
        {
            return new LocalCamera(index, deviceName: name ?? $"Local Camera #{index}");
        }

        /// <summary>
        /// Перечислить доступные локальные камеры (пробуем индексы от 0..N)
        /// </summary>
        public static int[] EnumerateLocalCameraIndices(int maxProbe = 10)
        {
            var indices = new System.Collections.Generic.List<int>();
            for (int i = 0; i < maxProbe; i++)
            {
                using var cap = new VideoCapture(i);
                if (cap.IsOpened()) indices.Add(i);
            }
            return indices.ToArray();
        }
        /// <summary>
        /// Создать IP-камеру
        /// </summary>
        public static ICamera CreateIpCamera(string ipAddress, int port, string name = null)
        {
            return IpCamera.Create(ipAddress, port, name);
        }

        /// <summary>
        /// Создать USB-камеру
        /// </summary>
        public static ICamera CreateUsbCamera(int vendorId, int productId, string name = null)
        {
            return UsbCamera.Create(vendorId, productId, name);
        }

        /// <summary>
        /// Создать сетевой принтер
        /// </summary>
        public static IPrinter CreateNetworkPrinter(string ipAddress, int port = 9100, string name = null)
        {
            return NetworkPrinter.Create(ipAddress, port, name);
        }

        /// <summary>
        /// Создать USB принтер
        /// </summary>
        public static IPrinter CreateUsbPrinter(int vendorId, int productId, string name = null)
        {
            return UsbPrinter.Create(vendorId, productId, name);
        }

        /// <summary>
        /// Создать последовательный принтер (COM-порт)
        /// </summary>
        public static IPrinter CreateSerialPrinter(string portName, int baudRate = 9600, string name = null)
        {
            return SerialPrinter.Create(portName, baudRate, name);
        }

        /// <summary>
        /// Создать принтер по профилю и драйверу (подходит для разных вендоров/протоколов)
        /// </summary>
        public static IPrinter CreateDriverPrinter(
            IConnection connection,
            PrinterProfile profile,
            IPrinterDriver driver,
            string deviceId = null,
            string deviceName = null)
        {
            deviceId ??= $"DRV_PRN_{profile?.Manufacturer}_{profile?.Model}";
            deviceName ??= $"{profile?.Manufacturer} {profile?.Model}";
            return new DriverPrinter(deviceId, deviceName, connection, driver, profile);
        }

        /// <summary>
        /// Утилита: выбрать драйвер по строке протокола профиля
        /// </summary>
        public static IPrinterDriver ResolvePrinterDriver(PrinterProfile profile)
        {
            var proto = (profile?.Protocol ?? "").ToUpperInvariant();
            return proto switch
            {
                "BIXOLON" => new BixolonBk331Driver(),
                "ESC_POS" => new EscPosDriver(),
                _ => new EscPosDriver()
            };
        }

        // ============= OFFICE PRINTERS (Cross-platform) =============

        /// <summary>
        /// Создать офисный принтер (Windows/Linux) по системному имени
        /// </summary>
        public static IPrinter CreateOfficePrinter(string systemPrinterName, string deviceName = null)
        {
            var platformPrinter = GetPlatformPrinter();
            var deviceId = $"OFFICE_PRINTER_{systemPrinterName}";
            var name = deviceName ?? systemPrinterName;
            return new OfficePrinter(deviceId, name, systemPrinterName, platformPrinter);
        }

        /// <summary>
        /// Получить список доступных офисных принтеров в системе
        /// </summary>
        public static async System.Threading.Tasks.Task<string[]> GetAvailableOfficePrintersAsync()
        {
            var platformPrinter = GetPlatformPrinter();
            return await platformPrinter.GetAvailablePrintersAsync();
        }

        // ============= SCANNERS (Cross-platform) =============

        /// <summary>
        /// Создать офисный сканер (Windows/Linux) по системному имени
        /// </summary>
        public static IScanner CreateOfficeScanner(string systemScannerName, string deviceName = null)
        {
            var platformScanner = GetPlatformScanner();
            var deviceId = $"OFFICE_SCANNER_{systemScannerName}";
            var name = deviceName ?? systemScannerName;
            return new OfficeScanner(deviceId, name, systemScannerName, platformScanner);
        }

        /// <summary>
        /// Получить список доступных сканеров в системе
        /// </summary>
        public static async System.Threading.Tasks.Task<string[]> GetAvailableScannersAsync()
        {
            var platformScanner = GetPlatformScanner();
            return await platformScanner.GetAvailableScannersAsync();
        }

        // ============= PLATFORM DETECTION =============

        private static IPlatformPrinter GetPlatformPrinter()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return new WindowsPlatformPrinter();
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return new LinuxPlatformPrinter();
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return new LinuxPlatformPrinter(); // macOS uses CUPS too
            else
                throw new PlatformNotSupportedException("Office printing not supported on this platform");
        }

        private static IPlatformScanner GetPlatformScanner()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return new WindowsPlatformScanner();
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return new LinuxPlatformScanner();
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return new LinuxPlatformScanner(); // macOS uses SANE/similar
            else
                throw new PlatformNotSupportedException("Scanning not supported on this platform");
        }

        /// <summary>
        /// Получить список доступных COM-портов
        /// </summary>
        public static string[] GetAvailableComPorts()
        {
            return SerialPrinter.GetAvailablePorts();
        }
    }

    /// <summary>
    /// Менеджер устройств для управления множеством устройств
    /// </summary>
    public class DeviceManager : IDisposable
    {
        private readonly Dictionary<string, IDevice> _devices = new Dictionary<string, IDevice>();
        private readonly object _devicesLock = new object();
        private bool _disposed = false;

        /// <summary>
        /// Добавить устройство в менеджер
        /// </summary>
        public void AddDevice(IDevice device)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));

            lock (_devicesLock)
            {
                if (_devices.ContainsKey(device.DeviceId))
                    throw new InvalidOperationException($"Устройство с ID '{device.DeviceId}' уже добавлено");

                _devices[device.DeviceId] = device;
            }
        }

        /// <summary>
        /// Удалить устройство из менеджера
        /// </summary>
        public bool RemoveDevice(string deviceId)
        {
            lock (_devicesLock)
            {
                if (_devices.TryGetValue(deviceId, out var device))
                {
                    device.Dispose();
                    _devices.Remove(deviceId);
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Получить устройство по ID
        /// </summary>
        public IDevice GetDevice(string deviceId)
        {
            lock (_devicesLock)
            {
                return _devices.TryGetValue(deviceId, out var device) ? device : null;
            }
        }

        /// <summary>
        /// Получить устройство определенного типа
        /// </summary>
        public T GetDevice<T>(string deviceId) where T : class, IDevice
        {
            return GetDevice(deviceId) as T;
        }

        /// <summary>
        /// Получить все устройства
        /// </summary>
        public IEnumerable<IDevice> GetAllDevices()
        {
            lock (_devicesLock)
            {
                return new List<IDevice>(_devices.Values);
            }
        }

        /// <summary>
        /// Получить все устройства определенного типа
        /// </summary>
        public IEnumerable<T> GetDevicesByType<T>() where T : class, IDevice
        {
            var result = new List<T>();
            lock (_devicesLock)
            {
                foreach (var device in _devices.Values)
                {
                    if (device is T typedDevice)
                        result.Add(typedDevice);
                }
            }
            return result;
        }

        /// <summary>
        /// Подключить все устройства
        /// </summary>
        public async System.Threading.Tasks.Task<bool> ConnectAllAsync()
        {
            bool allSuccess = true;
            foreach (var device in GetAllDevices())
            {
                try
                {
                    await device.ConnectAsync();
                }
                catch
                {
                    allSuccess = false;
                }
            }
            return allSuccess;
        }

        /// <summary>
        /// Отключить все устройства
        /// </summary>
        public async System.Threading.Tasks.Task DisconnectAllAsync()
        {
            foreach (var device in GetAllDevices())
            {
                try
                {
                    await device.DisconnectAsync();
                }
                catch
                {
                    // Игнорируем ошибки при отключении
                }
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            DisconnectAllAsync().Wait();

            lock (_devicesLock)
            {
                foreach (var device in _devices.Values)
                {
                    device.Dispose();
                }
                _devices.Clear();
            }

            _disposed = true;
        }
    }
}

