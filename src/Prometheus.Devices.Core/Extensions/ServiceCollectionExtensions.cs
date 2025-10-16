using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Prometheus.Devices.Core.Configuration;
using Prometheus.Devices.Core.Interfaces;

namespace Prometheus.Devices.Core.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add Prometheus.Devices with configuration
        /// </summary>
        public static IServiceCollection AddPrometheusDevicesCore(
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
        public static IServiceCollection AddPrometheusDevicesCore(this IServiceCollection services)
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
}

