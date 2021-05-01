using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Chatter.Application.Services;
using Chatter.ViewModels.Abstract;
using Chatter.ViewModels.Commands;

namespace Chatter.ViewModels
{
    public class MessagesViewModel : ViewModelBase
    {
        private readonly IDispatcher _dispatcher;
        private readonly IViewManager _viewManager;

        private readonly IMessageService _messageService;

        private string _message;

        public MessagesViewModel(IDispatcher dispatcher, IViewManager viewManager, IMessageService messageService,
            IServerService serverService, IClientService clientService)
        {
            if (serverService is null)
            {
                throw new ArgumentNullException(nameof(serverService));
            }

            if (clientService is null)
            {
                throw new ArgumentNullException(nameof(clientService));
            }

            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            _viewManager = viewManager ?? throw new ArgumentNullException(nameof(viewManager));

            _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));

            _message = string.Empty;
            MessageViewModels = new ObservableCollection<MessageViewModel>();

            SendMessageCommand = new AsyncCommand(SendMessageAsync, () => !string.IsNullOrWhiteSpace(Message));

            messageService.MessageReceived += OnMessageReceived;

            // Subscribe to connected and disconnected events, for when acting as a server.
            serverService.Connected += OnConnected;
            serverService.Disconnected += OnDisconnected;

            // Subscribe to connected and disconnected events, for when acting as a client.
            clientService.Connected += OnConnected;
            clientService.Disconnected += OnDisconnected;
        }

        public string Message
        {
            get => _message;
            set
            {
                Set(ref _message, value);

                // Inform the controls binding to the following command about the change of can execute.
                SendMessageCommand.RaiseCanExecuteChanged();
            }
        }

        public ObservableCollection<MessageViewModel> MessageViewModels { get; }

        public AsyncCommand SendMessageCommand { get; }

        private async Task SendMessageAsync()
        {
            if (string.IsNullOrWhiteSpace(Message))
            {
                return;
            }

            using var cancellationSource = new CancellationTokenSource(5_000);

            // Remove any leading and trailing white-space characters.
            string trimmedMessage = Message.Trim();

            try
            {
                await _messageService.SendMessageAsync(trimmedMessage, cancellationSource.Token);
                // The message has been sent, clear it from the UI.
                Message = string.Empty;
            }
            catch (Exception exception) when (exception is OperationCanceledException || exception is IOException)
            {
                await _viewManager.ShowErrorBoxAsync("Unable to send message due to network error.", "Unable To Send");

                return;
            }

            // Add the message as a sent message for display.
            var messageViewModel = new MessageViewModel(MessageType.Local, trimmedMessage);
            MessageViewModels.Add(messageViewModel);
        }

        private async void OnMessageReceived(object? sender, string message)
        {
            // This event handler is being executed on a thread pool thread, dispatch to the UI thread.
            await _dispatcher.InvokeAsync(() =>
            {
                // Add the message as a received message for display.
                var messageViewModel = new MessageViewModel(MessageType.Remote, message);
                MessageViewModels.Add(messageViewModel);
            });
        }

        private async void OnConnected(object? sender, EventArgs e)
        {
            // This event handler is being executed on a thread pool thread, dispatch to the UI thread.
            await _dispatcher.InvokeAsync(() =>
            {
                // Add a system message to indicate the successful connection.
                var messageViewModel = new MessageViewModel(MessageType.System, "You are now connected!");
                MessageViewModels.Add(messageViewModel);
            });
        }

        private async void OnDisconnected(object? sender, bool abortive)
        {
            // This event handler is being executed on a thread pool thread, dispatch to the UI thread.
            await _dispatcher.InvokeAsync(() =>
            {
                // Add a system message to indicate the graceful or abortive disconnection.
                var messageViewModel = new MessageViewModel(MessageType.System,
                    abortive ? "You have lost connection." : "You have been disconnected.");
                MessageViewModels.Add(messageViewModel);
            });
        }
    }
}
