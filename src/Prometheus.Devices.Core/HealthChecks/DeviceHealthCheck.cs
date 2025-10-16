using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Prometheus.Devices.Core.Extensions;
using Prometheus.Devices.Core.Interfaces;

namespace Prometheus.Devices.Core.HealthChecks
{
    public class DeviceHealthCheck : IHealthCheck
    {
        private readonly IDeviceManager _deviceManager;

        public DeviceHealthCheck(IDeviceManager deviceManager)
        {
            _deviceManager = deviceManager ?? throw new ArgumentNullException(nameof(deviceManager));
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var devices = _deviceManager.GetAllDevices().ToList();

                if (devices.Count == 0)
                    return HealthCheckResult.Degraded("No devices registered");

                var unhealthy = devices
                    .Where(d => d.Status == DeviceStatus.Error || d.Status == DeviceStatus.Disconnected)
                    .Select(d => $"{d.DeviceName}: {d.Status}")
                    .ToList();

                var data = new Dictionary<string, object>
                {
                    { "TotalDevices", devices.Count },
                    { "ReadyDevices", devices.Count(d => d.Status == DeviceStatus.Ready) },
                    { "ErrorDevices", devices.Count(d => d.Status == DeviceStatus.Error) }
                };

                if (unhealthy.Any())
                    return HealthCheckResult.Unhealthy($"Unhealthy: {string.Join(", ", unhealthy)}", data: data);

                return HealthCheckResult.Healthy($"All {devices.Count} devices healthy", data);
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Health check failed", ex);
            }
        }
    }

    public static class DeviceHealthCheckExtensions
    {
        public static IHealthChecksBuilder AddDeviceHealthCheck(
            this IHealthChecksBuilder builder,
            string name = "devices",
            HealthStatus? failureStatus = null,
            IEnumerable<string> tags = null)
        {
            return builder.AddCheck<DeviceHealthCheck>(name, failureStatus, tags);
        }
    }
}

