using System;
using System.Threading;
using System.Threading.Tasks;
using DeviceWrappers.Core.Interfaces;

namespace DeviceWrappers.Core.Connections
{
    /// <summary>
    /// Пустое локальное подключение для устройств, не требующих транспорта (встроенная камера и т.д.)
    /// </summary>
    public class NullConnection : BaseConnection
    {
        public override string ConnectionInfo => "LOCAL:null";

        public override Task OpenAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            SetStatus(ConnectionStatus.Connected, "Локальное подключение активно");
            return Task.CompletedTask;
        }

        public override Task CloseAsync(CancellationToken cancellationToken = default)
        {
            SetStatus(ConnectionStatus.Disconnected, "Локальное подключение закрыто");
            return Task.CompletedTask;
        }

        public override Task<int> SendAsync(byte[] data, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("NullConnection не поддерживает отправку данных");
        }

        public override Task<byte[]> ReceiveAsync(int bufferSize = 4096, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("NullConnection не поддерживает получение данных");
        }

        public override Task<bool> PingAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }
    }
}


