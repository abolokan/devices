namespace Prometheus.Devices.Core.Profiles
{
    /// <summary>
    /// Базовый профиль устройства, описывающий производителя/модель/версию и опции
    /// </summary>
    public class DeviceProfile
    {
        public string Manufacturer { get; set; }
        public string Model { get; set; }
        public string Version { get; set; }
        public string Protocol { get; set; } // например: ESC_POS, BIXOLON, ZPL, PJL
        public Dictionary<string, string> Options { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// Профиль принтера
    /// </summary>
    public class PrinterProfile : DeviceProfile
    {
        public int DefaultCodepage { get; set; } = 866; // справочная кодовая страница (например, PC866)
        public int? EscPosCodepage { get; set; } // значение 'n' для ESC t n (например, 17 для PC866)
        public int DefaultFeedLines { get; set; } = 2;
        public bool SupportsCut { get; set; } = false;
        public bool PartialCut { get; set; } = false;
    }
}