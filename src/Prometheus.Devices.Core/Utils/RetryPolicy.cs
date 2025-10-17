using Prometheus.Devices.Abstractions.Utils;

namespace Prometheus.Devices.Core.Utils
{
    public class RetryPolicy
    {
        public int MaxRetries { get; set; } = 3;
        public int DelayMs { get; set; } = 1000;
        public bool ExponentialBackoff { get; set; } = true;
        public Func<Exception, bool> ShouldRetry { get; set; }

        public RetryPolicy()
        {
            ShouldRetry = ex => 
                ex is DeviceTimeoutException || 
                ex is DeviceBusyException ||
                (ex is DeviceException deviceEx && deviceEx.ErrorCode == ErrorCode.ConnectionFailed);
        }

        public async Task<T> ExecuteAsync<T>(
            Func<Task<T>> operation, 
            CancellationToken cancellationToken = default)
        {
            int attempt = 0;
            Exception? lastException = null;

            while (attempt < MaxRetries)
            {
                try
                {
                    return await operation();
                }
                catch (Exception ex) when (ShouldRetry(ex) && attempt < MaxRetries - 1)
                {
                    lastException = ex;
                    attempt++;

                    int delay = ExponentialBackoff 
                        ? DelayMs * (int)Math.Pow(2, attempt - 1)
                        : DelayMs;

                    await Task.Delay(delay, cancellationToken);
                }
            }

            throw new DeviceException(
                $"Operation failed after {MaxRetries} attempts", 
                lastException);
        }

        public async Task ExecuteAsync(
            Func<Task> operation, 
            CancellationToken cancellationToken = default)
        {
            await ExecuteAsync(async () =>
            {
                await operation();
                return true;
            }, cancellationToken);
        }
    }
}

