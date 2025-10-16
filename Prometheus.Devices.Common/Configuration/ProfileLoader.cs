using System.Text.Json;
using Prometheus.Devices.Core.Profiles;

namespace Prometheus.Devices.Common.Configuration
{
    public static class ProfileLoader
    {
        public static PrinterProfile LoadPrinterProfile(string jsonPath)
        {
            var json = File.ReadAllText(jsonPath);
            var profile = JsonSerializer.Deserialize<PrinterProfile>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return profile ?? throw new InvalidOperationException($"Failed to load profile from {jsonPath}");
        }
    }
}

