using System.Text;

namespace Prometheus.Devices.Abstractions.Drivers
{
    /// <summary>
    /// Printer driver that builds byte commands for specific protocol/vendor
    /// </summary>
    public interface IPrinterDriver
    {
        /// <summary>
        /// Initialize command buffer (e.g., ESC @)
        /// </summary>
        byte[] BuildInitialize();

        /// <summary>
        /// Set print encoding (code table)
        /// </summary>
        byte[] BuildSetCodepage(int codepageId);

        /// <summary>
        /// Print text
        /// </summary>
        byte[] BuildPrintText(string text, Encoding encoding);

        /// <summary>
        /// Feed lines
        /// </summary>
        byte[] BuildFeedLines(int lines);

        /// <summary>
        /// Paper cut (partial = partial cut)
        /// </summary>
        byte[] BuildCut(bool partial);

        /// <summary>
        /// Raw data
        /// </summary>
        byte[] BuildRaw(byte[] data);

        /// <summary>
        /// Print barcode
        /// </summary>
        byte[] BuildPrintBarcode(string data, BarcodeType type, int height = 100, int width = 3);

        /// <summary>
        /// Print QR code
        /// </summary>
        byte[] BuildPrintQrCode(string data, int size = 6, QrErrorCorrection errorLevel = QrErrorCorrection.M);
    }

    /// <summary>
    /// Barcode types
    /// </summary>
    public enum BarcodeType
    {
        Code39,
        Code128,
        EAN13,
        EAN8,
        UPCA,
        UPCE,
        ITF,
        Codabar
    }

    /// <summary>
    /// QR code error correction level
    /// </summary>
    public enum QrErrorCorrection
    {
        L = 0x30, // Low (7%)
        M = 0x31, // Medium (15%)
        Q = 0x32, // Quality (25%)
        H = 0x33  // High (30%)
    }
}

