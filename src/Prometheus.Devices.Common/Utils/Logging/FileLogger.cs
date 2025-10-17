using System.Text;

namespace Prometheus.Devices.Common.Utils.Logging
{
    /// <summary>
    /// File logger implementation
    /// </summary>
    public class FileLogger : IDeviceLogger
    {
        private readonly string _category;
        private readonly string _logFilePath;
        private readonly LogLevel _minLevel;
        private readonly object _fileLock = new object();

        public FileLogger(string category, string logFilePath, LogLevel minLevel = LogLevel.Info)
        {
            _category = category;
            _logFilePath = logFilePath;
            _minLevel = minLevel;

            // Create directory if it doesn't exist
            var directory = Path.GetDirectoryName(logFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
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

            var sb = new StringBuilder();
            sb.AppendLine($"[{timestamp}] [{levelStr}] [{_category}] {message}");

            if (exception != null)
            {
                sb.AppendLine($"  Exception: {exception.GetType().Name}: {exception.Message}");
                sb.AppendLine($"  StackTrace: {exception.StackTrace}");
            }

            lock (_fileLock)
            {
                try
                {
                    File.AppendAllText(_logFilePath, sb.ToString(), Encoding.UTF8);
                }
                catch
                {
                    // Cannot log logging error
                }
            }
        }
    }
}

