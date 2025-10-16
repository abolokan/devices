using System.IO;
using System.Text.Json;
using DeviceWrappers.Core.Profiles;

namespace DeviceWrappers.Utils
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
            return profile;
        }
    }
}


