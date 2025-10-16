using System.Text;

namespace Prometheus.Devices.Core.Drivers
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
    }
}
