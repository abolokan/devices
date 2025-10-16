using System;
using System.Threading.Tasks;
using DeviceWrappers.Core.Interfaces;
using DeviceWrappers.Utils;
using DeviceWrappers.Devices.Camera;
using DeviceWrappers.Core.Profiles;
using DeviceWrappers.Core.Drivers;
using DeviceWrappers.Drivers.Printers;

namespace DeviceWrappers.Examples
{
    public class Program
    {
        public static async Task Main()
        {
            // Пример использования фабрики устройств и базовых операций
            // Локальная встроенная камера (индекс 0)
            ICamera camera = DeviceFactory.CreateLocalCamera(0, "Built-in Cam");
            IPrinter printer = DeviceFactory.CreateNetworkPrinter("192.168.1.50", 9100, "Net Printer");

            try
            {
                await camera.ConnectAsync();
                await camera.InitializeAsync();
                var frame = await camera.CaptureFrameAsync();
                await camera.SaveFrameAsync(frame, "frame.jpg");
                
                // Пример непрерывного потока
                camera.FrameCaptured += (s, e) => {
                    // e.Frame.Data содержит картинку (JPEG/PNG)
                };
                await camera.StartStreamingAsync();
                await Task.Delay(2000);
                await camera.StopStreamingAsync();

                // Пример простого сетевого принтера оставлен для совместимости,
                // рекомендуемый путь для production — драйверный принтер ниже.
                await printer.ConnectAsync();
                await printer.InitializeAsync();
                await printer.PrintTextAsync("Test print from DeviceWrappers");

                // Пример печати через профиль/драйвер
                var profilePath = System.IO.Path.Combine(AppContext.BaseDirectory, "printer.profile.json");
                if (System.IO.File.Exists(profilePath))
                {
                    var profile = ProfileLoader.LoadPrinterProfile(profilePath);
                    IPrinterDriver driver = DeviceFactory.ResolvePrinterDriver(profile);

                    // Создадим сетевое подключение для демонстрации
                    var drvPrinter = DeviceFactory.CreateDriverPrinter(
                        new DeviceWrappers.Core.Connections.TcpConnection("192.168.1.50", 9100),
                        profile,
                        driver,
                        deviceName: $"{profile.Manufacturer} {profile.Model} (Driver)");

                    await drvPrinter.ConnectAsync();
                    await drvPrinter.InitializeAsync();
                    await drvPrinter.PrintTextAsync("Driver-based print example");
                    await drvPrinter.DisconnectAsync();
                    drvPrinter.Dispose();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка выполнения примера: {ex.Message}");
            }
            finally
            {
                await camera.DisconnectAsync();
                await printer.DisconnectAsync();
                camera.Dispose();
                printer.Dispose();
            }
        }
    }
}


