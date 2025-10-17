using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Prometheus.Devices.Abstractions.Interfaces;

namespace Prometheus.Devices.Infrastructure.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add Prometheus.Devices with configuration
        /// </summary>
        public static IServiceCollection AddPrometheusDevices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<PrometheusDevicesOptions>(
                configuration.GetSection("PrometheusDevices"));
            
            services.TryAddSingleton<IDeviceManager, DeviceManager>();
            return services;
        }
        
        /// <summary>
        /// Add Prometheus.Devices without configuration
        /// </summary>
        public static IServiceCollection AddPrometheusDevices(this IServiceCollection services)
        {
            services.TryAddSingleton<IDeviceManager, DeviceManager>();
            return services;
        }
    }

    public interface IDeviceManager
    {
        void RegisterDevice(IDevice device);
        void UnregisterDevice(string deviceId);
        IDevice GetDevice(string deviceId);
        T GetDevice<T>(string deviceId) where T : class, IDevice;
        IEnumerable<IDevice> GetAllDevices();
        IEnumerable<T> GetDevicesByType<T>() where T : class, IDevice;
        Task ConnectAllAsync();
        Task DisconnectAllAsync();
    }

    public class DeviceManager : IDeviceManager, IDisposable
    {
        private readonly Dictionary<string, IDevice> _devices = new();
        private readonly object _lock = new();
        private bool _disposed;

        public void RegisterDevice(IDevice device)
        {
            if (device == null) throw new ArgumentNullException(nameof(device));
            lock (_lock) { _devices[device.DeviceId] = device; }
        }

        public void UnregisterDevice(string deviceId)
        {
            lock (_lock)
            {
                if (_devices.Remove(deviceId, out var device))
                    device.Dispose();
            }
        }

        public IDevice GetDevice(string deviceId)
        {
            lock (_lock)
                return _devices.TryGetValue(deviceId, out var device) ? device : null;
        }

        public T GetDevice<T>(string deviceId) where T : class, IDevice => GetDevice(deviceId) as T;

        public IEnumerable<IDevice> GetAllDevices()
        {
            lock (_lock) return _devices.Values.ToList();
        }

        public IEnumerable<T> GetDevicesByType<T>() where T : class, IDevice
        {
            lock (_lock) return _devices.Values.OfType<T>().ToList();
        }

        public async Task ConnectAllAsync()
        {
            foreach (var device in GetAllDevices())
                try { await device.ConnectAsync(); } catch {  }
        }

        public async Task DisconnectAllAsync()
        {
            foreach (var device in GetAllDevices())
                try { await device.DisconnectAsync(); } catch {  }
        }

        public void Dispose()
        {
            if (_disposed) return;
            DisconnectAllAsync().Wait();
            lock (_lock)
            {
                foreach (var device in _devices.Values)
                    device?.Dispose();
                _devices.Clear();
            }
            _disposed = true;
        }
    }

    public class PrometheusDevicesOptions
    {
        public Dictionary<string, CameraOptions> Cameras { get; set; } = new();
        public Dictionary<string, PrinterOptions> Printers { get; set; } = new();
        public Dictionary<string, ScannerOptions> Scanners { get; set; } = new();
    }

    public class CameraOptions
    {
        public string Type { get; set; } = "Local";
        public int? Index { get; set; }
        public string? IpAddress { get; set; }
        public int? Port { get; set; }
        public int? VendorId { get; set; }
        public int? ProductId { get; set; }
        public string Resolution { get; set; } = "1920x1080";
        public int FrameRate { get; set; } = 30;
        public bool Enabled { get; set; } = true;
    }

    public class PrinterOptions
    {
        public string Type { get; set; } = "Office";
        public string? ProfilePath { get; set; }
        public string? SystemPrinterName { get; set; }
        public string? IpAddress { get; set; }
        public int Port { get; set; } = 9100;
        public string? PortName { get; set; }
        public int BaudRate { get; set; } = 9600;
        public int? VendorId { get; set; }
        public int? ProductId { get; set; }
        public bool Enabled { get; set; } = true;
    }

    public class ScannerOptions
    {
        public string Type { get; set; } = "Office";
        public string? SystemScannerName { get; set; }
        public int Resolution { get; set; } = 300;
        public string ColorMode { get; set; } = "Color";
        public bool Enabled { get; set; } = true;
    }
}


