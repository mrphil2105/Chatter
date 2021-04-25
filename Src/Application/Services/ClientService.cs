using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using TcpClient = Chatter.Application.Internal.TcpClient;

namespace Chatter.Application.Services
{
    internal class ClientService : IClientService, IDisposable
    {
        private TcpClient? _tcpClient;

        public event EventHandler? Connected;

        public event EventHandler<bool>? Disconnected;

        public async Task<Task> ConnectAsync(IPAddress address, int port, CancellationToken cancellationToken = default)
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
            cancellationToken.ThrowIfCancellationRequested();

            if (_tcpClient != null)
            {
                throw new InvalidOperationException("The client is already connected.");
            }

            var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                await socket.ConnectAsync(address, port, cancellationToken)
                    .ConfigureAwait(false);

                // Wrap the connected socket in a 'TcpClient' instance.
                _tcpClient = new TcpClient(socket);
                _tcpClient.Disconnected += OnDisconnected;

                Connected?.Invoke(this, EventArgs.Empty);

                // Start receiving data in a loop and return the task as a representation of the connection.
                return _tcpClient.LoopReceiveDataAsync();
            }
            catch
            {
                // Something went wrong, we should release the socket.
                socket.Dispose();

                throw;
            }
        }

        public void Disconnect(bool force = false)
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
            // Unsubscribe to clear the reference and set to 'null' to allow new calls to 'ConnectAsync'.
            _tcpClient!.Disconnected -= OnDisconnected;
            _tcpClient.Dispose();
            _tcpClient = null;
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

            _isDisposed = true;
        }
    }
}
