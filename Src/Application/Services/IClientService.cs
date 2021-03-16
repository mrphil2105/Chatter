using System;
using System.Net;
using System.Threading.Tasks;

namespace Chatter.Application.Services
{
    public interface IClientService
    {
        event EventHandler<DisconnectedEventArgs> Disconnected;

        Task ConnectAsync(IPAddress address, int port);

        void Disconnect(bool force = false);
    }
}
