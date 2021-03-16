using System;

namespace Chatter.Application.Services
{
    public class DisconnectedEventArgs : EventArgs
    {
        public DisconnectedEventArgs(bool hasLostConnection)
        {
            HasLostConnection = hasLostConnection;
        }

        public bool HasLostConnection { get; }
    }
}
