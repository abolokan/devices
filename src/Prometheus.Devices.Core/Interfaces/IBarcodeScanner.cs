namespace Prometheus.Devices.Core.Interfaces
{
    /// <summary>
    /// Interface for working with barcode/QR scanners
    /// </summary>
    public interface IBarcodeScanner : IDevice
    {
        /// <summary>
        /// Current barcode scanner settings
        /// </summary>
        BarcodeScannerSettings Settings { get; set; }

        /// <summary>
        /// Barcode scanned event
        /// </summary>
        event EventHandler<BarcodeScannedEventArgs> BarcodeScanned;

        /// <summary>
        /// Start continuous scanning mode
        /// </summary>
        Task<bool> StartScanningAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Stop continuous scanning mode
        /// </summary>
        Task<bool> StopScanningAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Scan single barcode (trigger scan)
        /// </summary>
        Task<BarcodeData> ScanBarcodeAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Get supported barcode types
        /// </summary>
        Task<BarcodeType[]> GetSupportedBarcodesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Enable/disable specific barcode types
        /// </summary>
        Task<bool> SetEnabledBarcodesAsync(BarcodeType[] types, CancellationToken cancellationToken = default);

        /// <summary>
        /// Trigger beep sound
        /// </summary>
        Task<bool> BeepAsync(int durationMs = 100, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Barcode scanner settings
    /// </summary>
    public class BarcodeScannerSettings
    {
        /// <summary>
        /// Scan mode: Single or Continuous
        /// </summary>
        public ScanMode Mode { get; set; } = ScanMode.Single;

        /// <summary>
        /// Auto-beep on successful scan
        /// </summary>
        public bool BeepOnScan { get; set; } = true;

        /// <summary>
        /// Scan timeout in milliseconds (0 = no timeout)
        /// </summary>
        public int ScanTimeout { get; set; } = 5000;

        /// <summary>
        /// Enabled barcode types (null = all enabled)
        /// </summary>
        public BarcodeType[]? EnabledBarcodes { get; set; } = null;

        /// <summary>
        /// Trigger mode: Manual or Automatic
        /// </summary>
        public TriggerMode TriggerMode { get; set; } = TriggerMode.Manual;
    }

    /// <summary>
    /// Scan mode
    /// </summary>
    public enum ScanMode
    {
        /// <summary>
        /// Scan one barcode at a time
        /// </summary>
        Single,

        /// <summary>
        /// Continuous scanning
        /// </summary>
        Continuous
    }

    /// <summary>
    /// Trigger mode
    /// </summary>
    public enum TriggerMode
    {
        /// <summary>
        /// Manual trigger (button press)
        /// </summary>
        Manual,

        /// <summary>
        /// Automatic trigger (presentation mode)
        /// </summary>
        Automatic
    }

    /// <summary>
    /// Barcode types supported by scanner
    /// </summary>
    public enum BarcodeType
    {
        // 1D Barcodes
        Code39,
        Code93,
        Code128,
        EAN8,
        EAN13,
        UPCA,
        UPCE,
        Interleaved2of5,
        Codabar,
        GS1DataBar,
        
        // 2D Barcodes
        QRCode,
        DataMatrix,
        PDF417,
        Aztec,
        MaxiCode,
        
        // Postal Codes
        PostalCode,
        
        // All types
        All
    }

    /// <summary>
    /// Scanned barcode data
    /// </summary>
    public class BarcodeData
    {
        /// <summary>
        /// Decoded text from barcode
        /// </summary>
        public string Data { get; set; } = string.Empty;

        /// <summary>
        /// Type of barcode
        /// </summary>
        public BarcodeType Type { get; set; }

        /// <summary>
        /// Raw barcode data (bytes)
        /// </summary>
        public byte[]? RawData { get; set; }

        /// <summary>
        /// Timestamp of scan
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Quality/confidence of scan (0-100)
        /// </summary>
        public int Quality { get; set; }

        /// <summary>
        /// Scanner device ID that performed the scan
        /// </summary>
        public string? ScannerId { get; set; }
    }

    /// <summary>
    /// Barcode scanned event arguments
    /// </summary>
    public class BarcodeScannedEventArgs : EventArgs
    {
        public BarcodeData Barcode { get; set; } = new BarcodeData();
    }
}

