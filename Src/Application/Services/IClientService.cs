using System;
using System.Net;
using System.Threading.Tasks;

namespace Chatter.Application.Services
{
    public interface IClientService
    {
        event EventHandler Connected;

        event EventHandler<bool> Disconnected;

        Task ConnectAsync(IPAddress address, int port);

        void Disconnect(bool force = false);
    }
}
