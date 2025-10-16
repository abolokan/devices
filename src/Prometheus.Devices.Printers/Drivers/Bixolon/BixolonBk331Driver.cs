using System.Text;
using Prometheus.Devices.Printers.Drivers.EscPos;

namespace Prometheus.Devices.Printers.Drivers.Bixolon
{
    /// <summary>
    /// Драйвер для Bixolon BK3-31 (на основе ESC/POS с особенностями).
    /// Детали команд: docs\Bixolon BK3-31 Datasheet.pdf
    /// </summary>
    public class BixolonBk331Driver : EscPosDriver
    {
        public override byte[] BuildInitialize()
        {
            return base.BuildInitialize();
        }

        public override byte[] BuildSetCodepage(int codepageId)
        {
            // ESC t n, для BK3-31: 17 = PC866 кириллица
            return base.BuildSetCodepage(codepageId);
        }

        public override byte[] BuildPrintText(string text, Encoding encoding)
        {
            return base.BuildPrintText(text, encoding);
        }
    }
}

