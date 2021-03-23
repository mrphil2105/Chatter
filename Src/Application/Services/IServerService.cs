using System;
using System.Net;
using System.Threading.Tasks;

namespace Chatter.Application.Services
{
    public interface IServerService
    {
        event EventHandler<bool> Disconnected;

        Task ListenAsync(IPAddress address, int port);

        void DropClient(bool force = false);
    }
}
