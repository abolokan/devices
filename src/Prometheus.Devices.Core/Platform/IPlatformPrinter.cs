namespace Prometheus.Devices.Core.Platform
{
    /// <summary>
    /// Platform-specific interface for printing
    /// </summary>
    public interface IPlatformPrinter
    {
        /// <summary>
        /// Get list of available printers in the system
        /// </summary>
        Task<string[]> GetAvailablePrintersAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Print text
        /// </summary>
        Task<string> PrintTextAsync(string printerName, string text, CancellationToken cancellationToken = default);

        /// <summary>
        /// Print file (PDF, TXT, image)
        /// </summary>
        Task<string> PrintFileAsync(string printerName, string filePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get printer status
        /// </summary>
        Task<bool> IsPrinterAvailableAsync(string printerName, CancellationToken cancellationToken = default);
    }
}
