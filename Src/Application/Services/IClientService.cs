using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Chatter.Application.Services
{
    public interface IClientService
    {
        event EventHandler Connected;

        event EventHandler<bool> Disconnected;

        Task<Task> ConnectAsync(IPAddress address, int port, CancellationToken cancellationToken = default);

        void Disconnect(bool force = false);
    }
}
