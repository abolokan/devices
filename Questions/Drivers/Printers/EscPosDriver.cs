using System.Text;
using DeviceWrappers.Core.Drivers;

namespace DeviceWrappers.Drivers.Printers
{
    /// <summary>
    /// Базовый ESC/POS драйвер (совместим с большинством чековых принтеров)
    /// </summary>
    public class EscPosDriver : IPrinterDriver
    {
        public virtual byte[] BuildInitialize() => new byte[] { 0x1B, 0x40 }; // ESC @

        public virtual byte[] BuildSetCodepage(int codepageId)
        {
            // ESC t n — выбор страницы кода/таблицы символов
            // 'codepageId' здесь — непосредственное значение 'n'
            return new byte[] { 0x1B, 0x74, (byte)codepageId };
        }

        public virtual byte[] BuildPrintText(string text, Encoding encoding)
        {
            var data = encoding.GetBytes(text.Replace("\n", "\r\n"));
            return data;
        }

        public virtual byte[] BuildFeedLines(int lines)
        {
            // ESC d n (прокрутить n строк)
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


