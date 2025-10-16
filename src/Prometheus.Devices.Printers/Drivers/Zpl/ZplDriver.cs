using System.Text;
using Prometheus.Devices.Core.Drivers;

namespace Prometheus.Devices.Printers.Drivers.Zpl
{
    /// <summary>
    /// ZPL (Zebra Programming Language) driver for Zebra label printers
    /// Cross-platform: Works on Windows, Linux, macOS
    /// Printers: Zebra ZD420, ZD620, ZT411, ZT231 and other ZPL-compatible
    /// </summary>
    public class ZplDriver : IPrinterDriver
    {
        public virtual byte[] BuildInitialize()
        {
            // ^XA - Start format, ^JMA - Set memory allocation, ^XZ - End format
            return Encoding.ASCII.GetBytes("^XA^JMA^XZ\n");
        }

        public virtual byte[] BuildSetCodepage(int codepageId)
        {
            // ^CI - Change International Font/Encoding
            // ^CI28 = UTF-8
            return Encoding.ASCII.GetBytes("^XA^CI28^XZ\n");
        }

        public virtual byte[] BuildPrintText(string text, Encoding encoding)
        {
            var zpl = new StringBuilder();
            zpl.Append("^XA\n");                          // Start format
            zpl.Append("^FO50,50\n");                     // Field origin (50, 50)
            zpl.Append($"^A0N,30,30\n");                  // Font (0, Normal, 30x30)
            zpl.Append($"^FD{text}^FS\n");                // Field data
            zpl.Append("^XZ\n");                          // End format
            
            return Encoding.UTF8.GetBytes(zpl.ToString());
        }

        public virtual byte[] BuildFeedLines(int lines)
        {
            // No direct equivalent in ZPL - labels are cut automatically
            // Use empty space instead
            int dots = lines * 10; // Approximate: 10 dots per line
            return Encoding.ASCII.GetBytes($"^XA^FO0,0^GB812,{dots},1^FS^XZ\n");
        }

        public virtual byte[] BuildCut(bool partial)
        {
            // ZPL cuts automatically after each label
            // ^MMT = Tear-off mode, ^MMC = Cutter mode
            return partial 
                ? Encoding.ASCII.GetBytes("^XA^MMT^XZ\n")
                : Encoding.ASCII.GetBytes("^XA^MMC^XZ\n");
        }

        public virtual byte[] BuildRaw(byte[] data) => data;

        /// <summary>
        /// Build barcode command (ZPL format)
        /// </summary>
        public virtual byte[] BuildPrintBarcode(string data, BarcodeType type, int height = 100, int width = 3)
        {
            var barcodeCmd = type switch
            {
                BarcodeType.Code39 => "^B3N",      // Code 39
                BarcodeType.Code128 => "^BCN",     // Code 128
                BarcodeType.EAN13 => "^BEN",       // EAN-13
                BarcodeType.EAN8 => "^B8N",        // EAN-8
                BarcodeType.UPCA => "^BUN",        // UPC-A
                BarcodeType.UPCE => "^B9N",        // UPC-E
                BarcodeType.ITF => "^B2N",         // Interleaved 2 of 5
                BarcodeType.Codabar => "^BKN",     // Codabar
                _ => "^BCN"                         // Default: Code 128
            };

            var zpl = new StringBuilder();
            zpl.Append("^XA\n");                                    // Start format
            zpl.Append("^FO100,100\n");                             // Position
            zpl.Append($"{barcodeCmd},{height},Y,N,N\n");           // Barcode command
            zpl.Append($"^FD{data}^FS\n");                          // Data
            zpl.Append("^XZ\n");                                    // End format

            return Encoding.ASCII.GetBytes(zpl.ToString());
        }

        /// <summary>
        /// Build QR code command (ZPL format)
        /// </summary>
        public virtual byte[] BuildPrintQrCode(string data, int size = 6, QrErrorCorrection errorLevel = QrErrorCorrection.M)
        {
            var errorChar = errorLevel switch
            {
                QrErrorCorrection.L => 'L',
                QrErrorCorrection.M => 'M',
                QrErrorCorrection.Q => 'Q',
                QrErrorCorrection.H => 'H',
                _ => 'M'
            };

            var zpl = new StringBuilder();
            zpl.Append("^XA\n");                                    // Start format
            zpl.Append("^FO100,100\n");                             // Position
            zpl.Append($"^BQN,2,{size}\n");                         // QR code command
            zpl.Append($"^FDMA,{data}^FS\n");                       // Data (MA = QR Model 2 Auto)
            zpl.Append("^XZ\n");                                    // End format

            return Encoding.ASCII.GetBytes(zpl.ToString());
        }

        /// <summary>
        /// Build complete label with title, barcode and text
        /// </summary>
        public byte[] BuildCompleteLabel(string title, string barcodeData, BarcodeType barcodeType, string[] lines = null)
        {
            var zpl = new StringBuilder();
            zpl.Append("^XA\n");                                    // Start format
            
            // Title
            zpl.Append("^FO50,30^A0N,40,40\n");                     // Large font for title
            zpl.Append($"^FD{title}^FS\n");

            // Barcode
            var barcodeCmd = barcodeType switch
            {
                BarcodeType.Code128 => "^BCN",
                BarcodeType.EAN13 => "^BEN",
                _ => "^BCN"
            };
            zpl.Append("^FO100,100\n");
            zpl.Append($"{barcodeCmd},100,Y,N,N\n");
            zpl.Append($"^FD{barcodeData}^FS\n");

            // Additional lines
            if (lines != null)
            {
                int y = 250;
                foreach (var line in lines)
                {
                    zpl.Append($"^FO50,{y}^A0N,25,25\n");
                    zpl.Append($"^FD{line}^FS\n");
                    y += 35;
                }
            }

            zpl.Append("^XZ\n");                                    // End format

            return Encoding.ASCII.GetBytes(zpl.ToString());
        }
    }
}

