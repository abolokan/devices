namespace Prometheus.Devices.Abstractions.Utils
{
    public class DeviceException : Exception
    {
        public string? DeviceId { get; }
        public string? DeviceName { get; }
        public ErrorCode ErrorCode { get; }

        public DeviceException(string message) : base(message)
        {
            ErrorCode = ErrorCode.Unknown;
        }

        public DeviceException(string message, Exception? innerException) 
            : base(message, innerException)
        {
            ErrorCode = ErrorCode.Unknown;
        }

        public DeviceException(string deviceId, string deviceName, string message, ErrorCode errorCode = ErrorCode.Unknown)
            : base($"[{deviceName}:{deviceId}] {message}")
        {
            DeviceId = deviceId;
            DeviceName = deviceName;
            ErrorCode = errorCode;
        }

        public DeviceException(string deviceId, string deviceName, string message, Exception innerException, ErrorCode errorCode = ErrorCode.Unknown)
            : base($"[{deviceName}:{deviceId}] {message}", innerException)
        {
            DeviceId = deviceId;
            DeviceName = deviceName;
            ErrorCode = errorCode;
        }
    }

    public class DeviceInitializationException : DeviceException
    {
        public DeviceInitializationException(string deviceId, string deviceName, string message)
            : base(deviceId, deviceName, message, ErrorCode.InitializationFailed)
        {
        }

        public DeviceInitializationException(string deviceId, string deviceName, string message, Exception innerException)
            : base(deviceId, deviceName, message, innerException, ErrorCode.InitializationFailed)
        {
        }
    }

    public class DeviceTimeoutException : DeviceException
    {
        public int TimeoutMs { get; }

        public DeviceTimeoutException(string deviceId, string deviceName, int timeoutMs)
            : base(deviceId, deviceName, $"Timeout exceeded ({timeoutMs}ms)", ErrorCode.Timeout)
        {
            TimeoutMs = timeoutMs;
        }
    }

    public class DeviceBusyException : DeviceException
    {
        public DeviceBusyException(string deviceId, string deviceName, string operation)
            : base(deviceId, deviceName, $"Device busy, cannot execute: {operation}", ErrorCode.DeviceBusy)
        {
        }
    }

    public enum ErrorCode
    {
        Unknown = 0,
        InitializationFailed = 100,
        ConnectionFailed = 101,
        Timeout = 200,
        DeviceBusy = 201,
        DeviceNotReady = 202,
        InvalidParameter = 300,
        InvalidData = 301,
        NotSupported = 400,
        HardwareError = 500,
        FirmwareError = 501
    }
}


