using System.Text;
using Prometheus.Devices.Abstractions.Drivers;

namespace Prometheus.Devices.Printers.Drivers.EscPos
{
    /// <summary>
    /// Basic ESC/POS driver (compatible with most receipt printers like Epson, Bixolon, Star, Citizen)
    /// Cross-platform: Works on Windows, Linux, macOS
    /// </summary>
    public class EscPosDriver : IPrinterDriver
    {
        public virtual byte[] BuildInitialize() => new byte[] { 0x1B, 0x40 }; // ESC @

        public virtual byte[] BuildSetCodepage(int codepageId)
        {
            // ESC t n — select code page/character table
            return new byte[] { 0x1B, 0x74, (byte)codepageId };
        }

        public virtual byte[] BuildPrintText(string text, Encoding encoding)
        {
            var data = encoding.GetBytes(text.Replace("\n", "\r\n"));
            return data;
        }

        public virtual byte[] BuildFeedLines(int lines)
        {
            // ESC d n
            return new byte[] { 0x1B, 0x64, (byte)lines };
        }

        public virtual byte[] BuildCut(bool partial)
        {
            // GS V m
            return partial ? new byte[] { 0x1D, 0x56, 0x01 } : new byte[] { 0x1D, 0x56, 0x00 };
        }

        public virtual byte[] BuildRaw(byte[] data) => data;

        /// <summary>
        /// Build barcode print command (ESC/POS standard)
        /// </summary>
        public virtual byte[] BuildPrintBarcode(string data, BarcodeType type, int height = 100, int width = 3)
        {
            var cmd = new List<byte>();

            // GS h n - Set barcode height (1-255 dots)
            cmd.AddRange(new byte[] { 0x1D, 0x68, (byte)Math.Clamp(height, 1, 255) });

            // GS w n - Set barcode width (2-6)
            cmd.AddRange(new byte[] { 0x1D, 0x77, (byte)Math.Clamp(width, 2, 6) });

            // GS H n - HRI (Human Readable Interpretation) position
            cmd.AddRange(new byte[] { 0x1D, 0x48, 0x02 }); // Below barcode

            // GS k m n d1...dn - Print barcode
            cmd.Add(0x1D); // GS
            cmd.Add(0x6B); // k

            byte barcodeType = type switch
            {
                BarcodeType.Code39 => 0x04,
                BarcodeType.Code128 => 0x49,
                BarcodeType.EAN13 => 0x02,
                BarcodeType.EAN8 => 0x03,
                BarcodeType.UPCA => 0x00,
                BarcodeType.UPCE => 0x01,
                BarcodeType.ITF => 0x05,
                BarcodeType.Codabar => 0x06,
                _ => 0x04 // Default to Code39
            };

            cmd.Add(barcodeType);
            cmd.Add((byte)data.Length);
            cmd.AddRange(Encoding.ASCII.GetBytes(data));

            return cmd.ToArray();
        }

        /// <summary>
        /// Build QR code print command (ESC/POS standard GS ( k)
        /// </summary>
        public virtual byte[] BuildPrintQrCode(string data, int size = 6, QrErrorCorrection errorLevel = QrErrorCorrection.M)
        {
            var cmd = new List<byte>();
            byte[] qrData = Encoding.UTF8.GetBytes(data);

            // Function 165: Set QR code model
            cmd.AddRange(new byte[] { 0x1D, 0x28, 0x6B, 0x04, 0x00, 0x31, 0x41, 0x32, 0x00 }); // Model 2

            // Function 167: Set module size (1-16)
            size = Math.Clamp(size, 1, 16);
            cmd.AddRange(new byte[] { 0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x43, (byte)size });

            // Function 169: Set error correction level
            cmd.AddRange(new byte[] { 0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x45, (byte)errorLevel });

            // Function 180: Store QR code data
            int dataLen = qrData.Length + 3;
            cmd.AddRange(new byte[] { 
                0x1D, 0x28, 0x6B,
                (byte)(dataLen & 0xFF), 
                (byte)((dataLen >> 8) & 0xFF),
                0x31, 0x50, 0x30
            });
            cmd.AddRange(qrData);

            // Function 181: Print QR code
            cmd.AddRange(new byte[] { 0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x51, 0x30 });

            return cmd.ToArray();
        }
    }
}

