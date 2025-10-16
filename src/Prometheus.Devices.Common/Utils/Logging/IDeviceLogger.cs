namespace DeviceWrappers.Utils.Logging
{
    /// <summary>
    /// Interface for logging device events
    /// </summary>
    public interface IDeviceLogger
    {
        void LogDebug(string message);
        void LogInfo(string message);
        void LogWarning(string message);
        void LogError(string message, Exception exception = null);
        void LogCritical(string message, Exception exception = null);
    }

    /// <summary>
    /// Logging level
    /// </summary>
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error,
        Critical
    }
}

