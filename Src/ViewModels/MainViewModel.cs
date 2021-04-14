using System;

namespace Chatter.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        public MainViewModel(ConnectionViewModel connectionViewModel, MessagesViewModel messagesViewModel)
        {
            ConnectionViewModel = connectionViewModel ?? throw new ArgumentNullException(nameof(connectionViewModel));
            MessagesViewModel = messagesViewModel ?? throw new ArgumentNullException(nameof(messagesViewModel));
        }

        public ConnectionViewModel ConnectionViewModel { get; }

        public MessagesViewModel MessagesViewModel { get; }
    }
}
