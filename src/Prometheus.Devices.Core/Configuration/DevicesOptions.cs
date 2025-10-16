namespace Prometheus.Devices.Core.Configuration
{
    public class PrometheusDevicesOptions
    {
        public Dictionary<string, CameraOptions> Cameras { get; set; } = new();
        public Dictionary<string, PrinterOptions> Printers { get; set; } = new();
        public Dictionary<string, ScannerOptions> Scanners { get; set; } = new();
    }

    public class CameraOptions
    {
        public string Type { get; set; } // "Local", "IP", "USB"
        public int? Index { get; set; }
        public string IpAddress { get; set; }
        public int? Port { get; set; }
        public int? VendorId { get; set; }
        public int? ProductId { get; set; }
        public string Resolution { get; set; } = "1920x1080";
        public int FrameRate { get; set; } = 30;
        public bool Enabled { get; set; } = true;
    }

    public class PrinterOptions
    {
        public string Type { get; set; } // "Driver", "Office", "Network", "Serial", "USB"
        public string ProfilePath { get; set; }
        public string SystemPrinterName { get; set; }
        public string IpAddress { get; set; }
        public int Port { get; set; } = 9100;
        public string PortName { get; set; }
        public int BaudRate { get; set; } = 9600;
        public int? VendorId { get; set; }
        public int? ProductId { get; set; }
        public bool Enabled { get; set; } = true;
    }

    public class ScannerOptions
    {
        public string Type { get; set; } // "Office"
        public string SystemScannerName { get; set; }
        public int Resolution { get; set; } = 300;
        public string ColorMode { get; set; } = "Color";
        public bool Enabled { get; set; } = true;
    }
}

