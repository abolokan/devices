using System.Text;

namespace DeviceWrappers.Drivers.Printers
{
    /// <summary>
    /// Драйвер для Bixolon BK3-31 (на основе ESC/POS с особенностями).
    /// Детали команд уточняйте в datasheet (docs\Bixolon BK3-31 Datasheet.pdf).
    /// </summary>
    public class BixolonBk331Driver : EscPosDriver
    {
        public override byte[] BuildInitialize()
        {
            // Общая инициализация ESC @
            return base.BuildInitialize();
        }

        public override byte[] BuildSetCodepage(int codepageId)
        {
            // Для BK3-31 применяем ESC t n
            // Значение n должно соответствовать таблице в datasheet (например, 17 — PC866 для кириллицы, зависит от прошивки)
            return base.BuildSetCodepage(codepageId);
        }

        public override byte[] BuildPrintText(string text, Encoding encoding)
        {
            // У Bixolon допустимы специфичные команды форматирования; пока печатаем как в ESC/POS
            return base.BuildPrintText(text, encoding);
        }
    }
}


