using System;
using System.ComponentModel;

namespace Chatter.ViewModels
{
    public class MessageViewModel : ViewModelBase
    {
        public MessageViewModel(MessageType type, string message)
        {
            if (!Enum.IsDefined(typeof(MessageType), type))
            {
                throw new InvalidEnumArgumentException(nameof(type), (int)type, typeof(MessageType));
            }

            Type = type;
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }

        public MessageType Type { get; }

        public string Message { get; }
    }
}
