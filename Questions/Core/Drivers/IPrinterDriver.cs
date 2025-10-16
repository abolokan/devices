using System.Text;

namespace DeviceWrappers.Core.Drivers
{
    /// <summary>
    /// Драйвер принтера, формирующий байтовые команды под конкретный протокол/вендора
    /// </summary>
    public interface IPrinterDriver
    {
        /// <summary>
        /// Инициализировать буфер команд (например, ESC @)
        /// </summary>
        byte[] BuildInitialize();

        /// <summary>
        /// Установить кодировку печати (таблица кодов)
        /// </summary>
        byte[] BuildSetCodepage(int codepageId);

        /// <summary>
        /// Печать текста
        /// </summary>
        byte[] BuildPrintText(string text, Encoding encoding);

        /// <summary>
        /// Прокрутка на lines строк
        /// </summary>
        byte[] BuildFeedLines(int lines);

        /// <summary>
        /// Обрезка бумаги (partial = частичная)
        /// </summary>
        byte[] BuildCut(bool partial);

        /// <summary>
        /// Сырые данные
        /// </summary>
        byte[] BuildRaw(byte[] data);
    }
}


