using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Prometheus.Devices.Test.App.Tests
{
    /// <summary>
    /// Health check tests for registered devices
    /// </summary>
    public static class HealthCheckTests
    {
        /// <summary>
        /// Test health checks for all registered devices
        /// </summary>
        public static async Task TestHealthCheckAsync(IServiceProvider serviceProvider)
        {
            Console.WriteLine();
            Console.WriteLine("=== HEALTH CHECK TEST ===");

            var healthCheckService = serviceProvider.GetRequiredService<HealthCheckService>();
            var result = await healthCheckService.CheckHealthAsync();

            Console.WriteLine($"Overall Status: {result.Status}");
            Console.WriteLine($"Total Duration: {result.TotalDuration.TotalMilliseconds:F2} ms");
            Console.WriteLine();

            if (result.Entries.Count == 0)
            {
                Console.WriteLine("No health checks registered.");
                Console.WriteLine("Note: Register devices first using other test options.");
                return;
            }

            Console.WriteLine($"Health Checks ({result.Entries.Count}):");
            foreach (var entry in result.Entries)
            {
                var statusIcon = entry.Value.Status switch
                {
                    HealthStatus.Healthy => "✓",
                    HealthStatus.Degraded => "⚠",
                    HealthStatus.Unhealthy => "✗",
                    _ => "?"
                };

                Console.WriteLine($"  {statusIcon} {entry.Key}: {entry.Value.Status}");
                
                if (entry.Value.Description != null)
                    Console.WriteLine($"    Description: {entry.Value.Description}");
                
                Console.WriteLine($"    Duration: {entry.Value.Duration.TotalMilliseconds:F2} ms");
                
                if (entry.Value.Exception != null)
                    Console.WriteLine($"    Error: {entry.Value.Exception.Message}");
                
                if (entry.Value.Data.Count > 0)
                {
                    Console.WriteLine("    Data:");
                    foreach (var data in entry.Value.Data)
                        Console.WriteLine($"      {data.Key}: {data.Value}");
                }
            }
        }
    }
}

