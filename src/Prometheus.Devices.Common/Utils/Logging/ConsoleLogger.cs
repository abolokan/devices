namespace Prometheus.Devices.Common.Utils.Logging
{
    /// <summary>
    /// Simple console logger implementation
    /// </summary>
    public class ConsoleLogger : IDeviceLogger
    {
        private readonly string _category;
        private readonly LogLevel _minLevel;

        public ConsoleLogger(string category, LogLevel minLevel = LogLevel.Info)
        {
            _category = category;
            _minLevel = minLevel;
        }

        public void LogDebug(string message)
        {
            if (_minLevel <= LogLevel.Debug)
                WriteLog(LogLevel.Debug, message);
        }

        public void LogInfo(string message)
        {
            if (_minLevel <= LogLevel.Info)
                WriteLog(LogLevel.Info, message);
        }

        public void LogWarning(string message)
        {
            if (_minLevel <= LogLevel.Warning)
                WriteLog(LogLevel.Warning, message);
        }

        public void LogError(string message, Exception exception = null)
        {
            if (_minLevel <= LogLevel.Error)
                WriteLog(LogLevel.Error, message, exception);
        }

        public void LogCritical(string message, Exception exception = null)
        {
            if (_minLevel <= LogLevel.Critical)
                WriteLog(LogLevel.Critical, message, exception);
        }

        private void WriteLog(LogLevel level, string message, Exception exception = null)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var levelStr = level.ToString().ToUpper().PadRight(8);
            
            var color = level switch
            {
                LogLevel.Debug => ConsoleColor.Gray,
                LogLevel.Info => ConsoleColor.White,
                LogLevel.Warning => ConsoleColor.Yellow,
                LogLevel.Error => ConsoleColor.Red,
                LogLevel.Critical => ConsoleColor.DarkRed,
                _ => ConsoleColor.White
            };

            Console.ForegroundColor = color;
            Console.WriteLine($"[{timestamp}] [{levelStr}] [{_category}] {message}");

            if (exception != null)
            {
                Console.WriteLine($"  Exception: {exception.GetType().Name}: {exception.Message}");
                Console.WriteLine($"  StackTrace: {exception.StackTrace}");
            }

            Console.ResetColor();
        }
    }
}

