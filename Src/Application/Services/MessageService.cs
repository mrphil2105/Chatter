using System;
using System.Buffers;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Chatter.Application.Internal;

namespace Chatter.Application.Services
{
    internal class MessageService : IMessageService
    {
        private TcpClient? _tcpClient;

        public MessageService(IServerService serverService, IClientService clientService)
        {
            if (serverService is not ServerService)
            {
                throw new ArgumentException($"Value must be of type '{typeof(ServerService).FullName}'.");
            }

            if (clientService is not ClientService)
            {
                throw new ArgumentException($"Value must be of type '{typeof(ClientService).FullName}'.");
            }

            serverService.Connected += OnConnected;
            serverService.Disconnected += OnDisconnected;

            clientService.Connected += OnConnected;
            clientService.Disconnected += OnDisconnected;
        }

        public event EventHandler<string>? MessageReceived;

        public async Task SendMessageAsync(string message, CancellationToken cancellationToken = default)
        {
            // Copy value to local variable because '_tcpClient' is accessed concurrently.
            var tcpClient = _tcpClient;

            if (tcpClient == null)
            {
                throw new InvalidOperationException("The server or client is not connected.");
            }

            // Get the length of the message in UTF-8 bytes.
            int byteCount = Encoding.UTF8.GetByteCount(message);

            // Rent some bytes to avoid allocation of a new byte array.
            using var memoryOwner = MemoryPool<byte>.Shared.Rent(byteCount);

            var messageBuffer = memoryOwner.Memory;
            Encoding.UTF8.GetBytes(message, messageBuffer.Span);

            await tcpClient.SendDataAsync(messageBuffer.Slice(0, byteCount), cancellationToken)
                .ConfigureAwait(false);
        }

        private void OnConnected(object sender, EventArgs e)
        {
            // Only one of either the 'ServerService' or 'ClientService' should be connected, not both at the same time.
            Debug.Assert(_tcpClient == null, $"Field '{nameof(_tcpClient)}' was unexpectedly not 'null'.");

            _tcpClient = (sender as ServerService)?.DangerousGetTcpClient() ??
                (sender as ClientService)?.DangerousGetTcpClient();

            if (_tcpClient != null)
            {
                _tcpClient.DataReceived += OnDataReceived;
            }
        }

        private void OnDisconnected(object sender, bool abortive)
        {
            _tcpClient!.DataReceived -= OnDataReceived;
            _tcpClient = null;
        }

        private void OnDataReceived(object sender, ReadOnlySequence<byte> messageBytes)
        {
            if (messageBytes.Length == 0)
            {
                // Ignore null messages.
                return;
            }

            string message = Encoding.UTF8.GetString(messageBytes);
            MessageReceived?.Invoke(this, message);
        }
    }
}
