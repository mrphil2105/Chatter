using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using TcpClient = Chatter.Application.Internal.TcpClient;

namespace Chatter.Application.Services
{
    internal class ServerService : IServerService, IDisposable
    {
        private Socket? _serverSocket;
        private TcpClient? _tcpClient;

        public event EventHandler? Connected;

        public event EventHandler<bool>? Disconnected;

        public async Task<Task> ListenAsync(IPAddress address, int port, CancellationToken cancellationToken = default)
        {
            if (address is null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            if (port < ushort.MinValue || port > ushort.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(port),
                    $"Value must be between {ushort.MinValue} and {ushort.MaxValue}.");
            }

            ThrowIfDisposed();

            if (_tcpClient != null)
            {
                throw new InvalidOperationException("The server is already connected to a client.");
            }

            if (_serverSocket != null)
            {
                throw new InvalidOperationException("The server is already listening for a client.");
            }

            _serverSocket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                _serverSocket.Bind(new IPEndPoint(address, port));
                // Since the server only communicates with one client, we set the backlog to zero.
                _serverSocket.Listen(0);

                Socket clientSocket;

                // 'AcceptAsync' does not support cancellation so instead we dispose to cancel.
                await using (cancellationToken.Register(_serverSocket.Dispose)
                    .ConfigureAwait(false))
                {
                    try
                    {
                        clientSocket = await _serverSocket.AcceptAsync()
                            .ConfigureAwait(false);
                    }
                    catch (SocketException) when (cancellationToken.IsCancellationRequested)
                    {
                        // A 'SocketException' means the socket has been disposed as part of a cancellation.
                        // Throw an 'OperationCanceledException' to signify the cancellation.
                        throw new OperationCanceledException(cancellationToken);
                    }
                }

                // Wrap the connected socket in a 'TcpClient' instance.
                _tcpClient = new TcpClient(clientSocket);
                _tcpClient.Disconnected += OnDisconnected;

                Connected?.Invoke(this, EventArgs.Empty);

                // Start receiving data in a loop and return the task as a representation of the connection.
                return _tcpClient.LoopReceiveDataAsync();
            }
            catch
            {
                // Something went wrong, we should release the socket.
                _serverSocket.Dispose();

                throw;
            }
        }

        public void DropClient(bool force = false)
        {
            ThrowIfDisposed();

            _tcpClient?.Disconnect(force);
        }

        public TcpClient? DangerousGetTcpClient()
        {
            return _tcpClient;
        }

        private void OnDisconnected(object? sender, bool abortive)
        {
            Reset();
            Disconnected?.Invoke(this, abortive);
        }

        private void Reset()
        {
            // Unsubscribe to clear the reference and set to 'null' to allow new calls to 'ListenAsync'.
            _tcpClient!.Disconnected -= OnDisconnected;
            _tcpClient.Dispose();
            _tcpClient = null;

            // Clean up the server socket and set to 'null' to allow new calls to 'ListenAsync'.
            _serverSocket!.Dispose();
            _serverSocket = null;
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(GetType()
                    .FullName);
            }
        }

        private bool _isDisposed;

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _tcpClient?.Dispose();
            _serverSocket?.Dispose();

            _isDisposed = true;
        }
    }
}
