using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Chatter.Application.Services
{
    public interface IServerService
    {
        event EventHandler Connected;

        event EventHandler<bool> Disconnected;

        Task<Task> ListenAsync(IPAddress address, int port, CancellationToken cancellationToken = default);

        void DropClient(bool force = false);
    }
}
