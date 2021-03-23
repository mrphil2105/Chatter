using System;
using System.Threading.Tasks;

namespace Chatter.Application.Services
{
    public interface IMessageService
    {
        event EventHandler<string> MessageReceived;

        Task SendMessageAsync(string message);
    }
}
