using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Chatter.Application.Services;
using Chatter.ViewModels.Abstract;
using Chatter.ViewModels.Commands;

namespace Chatter.ViewModels
{
    public class ConnectionViewModel : ViewModelBase, IDisposable
    {
        private readonly IDispatcher _dispatcher;
        private readonly IViewManager _viewManager;

        private readonly IServerService _serverService;
        private readonly IClientService _clientService;

        private CancellationTokenSource? _cancellationSource;

        private bool _isServer;
        private string _address;
        private int _port;

        private bool _isConnectingOrListening;
        private bool _isConnected;

        public ConnectionViewModel(IDispatcher dispatcher, IViewManager viewManager, IServerService serverService,
            IClientService clientService)
        {
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            _viewManager = viewManager ?? throw new ArgumentNullException(nameof(viewManager));

            _serverService = serverService ?? throw new ArgumentNullException(nameof(serverService));
            _clientService = clientService ?? throw new ArgumentNullException(nameof(clientService));

            _address = "0.0.0.0";
            _port = 8888;

#if DEBUG
            // Set default IP address to localhost for easier debugging.
            _address = "127.0.0.1";
#endif

            // Initialize the command properties for connecting and disconnecting.
            ConnectOrListenCommand = new AsyncCommand(ConnectOrListenAsync, () => !IsConnected);
            CancelOrDisconnectCommand =
                new RelayCommand(CancelOrDisconnect, () => IsConnectingOrListening || IsConnected);

            // Subscribe to connected and disconnected events, for when acting as a server.
            serverService.Connected += OnConnected;
            serverService.Disconnected += OnDisconnected;

            // Subscribe to connected and disconnected events, for when acting as a client.
            clientService.Connected += OnConnected;
            clientService.Disconnected += OnDisconnected;
        }

        // Used to indicate if the user wants to be server or client for the connection.
        public bool IsServer
        {
            get => _isServer;
            set => Set(ref _isServer, value);
        }

        public string Address
        {
            get => _address;
            set => Set(ref _address, value);
        }

        public int Port
        {
            get => _port;
            set => Set(ref _port, value);
        }

        public bool IsConnectingOrListening
        {
            get => _isConnectingOrListening;
            private set
            {
                Set(ref _isConnectingOrListening, value);

                // Inform the controls binding to the following command about the change of can execute.
                CancelOrDisconnectCommand.RaiseCanExecuteChanged();
            }
        }

        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                Set(ref _isConnected, value);

                // Inform the controls binding to the following commands about the change of can execute.
                ConnectOrListenCommand.RaiseCanExecuteChanged();
                CancelOrDisconnectCommand.RaiseCanExecuteChanged();
            }
        }

        public AsyncCommand ConnectOrListenCommand { get; }

        public RelayCommand CancelOrDisconnectCommand { get; }

        private async Task ConnectOrListenAsync()
        {
            if (!IPAddress.TryParse(Address, out var address))
            {
                await _viewManager.ShowErrorBoxAsync("The specified IP address is invalid.", "Invalid Address");

                return;
            }

            if (Port < ushort.MinValue || Port > ushort.MaxValue)
            {
                await _viewManager.ShowErrorBoxAsync("The specified port is invalid.", "Invalid Port");

                return;
            }

            IsConnectingOrListening = true;

            try
            {
                _cancellationSource?.Dispose();
                _cancellationSource = new CancellationTokenSource();

                if (IsServer)
                {
                    // Start listening for an incoming client on the specified address and port.
                    await _serverService.ListenAsync(address, Port, _cancellationSource.Token);

                    return;
                }

                // Attempt to connect to a remote user acting as server on the specified address and port.
                await _clientService.ConnectAsync(address, Port, _cancellationSource.Token);
            }
            catch (OperationCanceledException)
            {
                // Ignore 'OperationCanceledException' as it signifies cancellation.
            }
            finally
            {
                IsConnectingOrListening = false;
            }
        }

        private void CancelOrDisconnect()
        {
            if (!IsConnected)
            {
                _cancellationSource?.Cancel();

                return;
            }

            if (IsServer)
            {
                // Disconnect the client gracefully.
                _serverService.DropClient();

                return;
            }

            // Disconnect from the server gracefully.
            _clientService.Disconnect();
        }

        private async void OnConnected(object? sender, EventArgs e)
        {
            // This event handler is being executed on a thread pool thread, dispatch to the UI thread.
            await _dispatcher.InvokeAsync(() => IsConnected = true);
        }

        private async void OnDisconnected(object? sender, bool abortive)
        {
            // This event handler is being executed on a thread pool thread, dispatch to the UI thread.
            await _dispatcher.InvokeAsync(async () =>
            {
                IsConnected = false;

                if (abortive)
                {
                    await _viewManager.ShowErrorBoxAsync("The connection to the remote party has been lost.",
                        "Connection Lost");
                }
            });
        }

        public void Dispose()
        {
            _cancellationSource?.Dispose();
        }
    }
}
