namespace Prometheus.Devices.Core.Profiles
{
    /// <summary>
    /// Base device profile describing manufacturer/model/version and options
    /// </summary>
    public class DeviceProfile
    {
        public string Manufacturer { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string Version { get; set; } = "1.0";
        public string Protocol { get; set; } = "Generic";
        public Dictionary<string, string> Options { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// Printer profile
    /// </summary>
    public class PrinterProfile : DeviceProfile
    {
        public int DefaultCodepage { get; set; } = 866; // reference code page (e.g., PC866)
        public int? EscPosCodepage { get; set; } // value 'n' for ESC t n (e.g., 17 for PC866)
        public int DefaultFeedLines { get; set; } = 2;
        public bool SupportsCut { get; set; } = false;
        public bool PartialCut { get; set; } = false;
    }
}