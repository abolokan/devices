using System.Text;
using Prometheus.Devices.Core.Drivers;

namespace Prometheus.Devices.Printers.Drivers.EscPos
{
    /// <summary>
    /// Basic ESC/POS driver (compatible with most receipt printers)
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
    }
}

