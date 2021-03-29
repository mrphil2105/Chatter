using System;
using System.Buffers;
using System.Net.Sockets;
using System.Threading;

namespace Chatter.Application.Internal
{
    internal partial class TcpClient : IDisposable
    {
        private const int NullMessageInterval = 10_000;

#if DEBUG
        private const int DisconnectTimeout = Timeout.Infinite;
#else
        private const int DisconnectTimeout = 30_000;
#endif

        private readonly Socket _socket;
        private readonly NetworkStream _networkStream;
        private readonly SemaphoreSlim _sendLock;

        private readonly Timer _nullMessageTimer;
        private readonly Timer _disconnectTimer;

        private int _isConnected;

        public TcpClient(Socket socket)
        {
            _socket = socket;
            _networkStream = new NetworkStream(socket);
            _sendLock = new SemaphoreSlim(1, 1);

            _nullMessageTimer = new Timer(OnNullMessageTimerTick, null, NullMessageInterval, NullMessageInterval);
            _disconnectTimer = new Timer(OnDisconnectTimerTick, null, DisconnectTimeout, Timeout.Infinite);

            _isConnected = 1;
        }

        public event EventHandler<ReadOnlySequence<byte>>? DataReceived;

        public event EventHandler<bool>? Disconnected;

        public void Disconnect(bool abortive)
        {
            ThrowIfDisposed();

            if (abortive)
            {
                SetToDisconnected(true);

                return;
            }

            // Stop the null message timer.
            _nullMessageTimer.Change(Timeout.Infinite, Timeout.Infinite);

            try
            {
                // Initiate graceful shutdown by sending FD_CLOSE.
                _socket.Shutdown(SocketShutdown.Send);
            }
            catch (ObjectDisposedException)
            {
                // We ignore 'ObjectDisposedException' as that means the 'TcpClient' is already disconnected.
            }
        }

        private void SetToDisconnected(bool abortive)
        {
            // Thread-safe way to make sure only one concurrent call to 'SetToDisconnected' gets through.
            if (Interlocked.CompareExchange(ref _isConnected, 0, 1) != 1)
            {
                return;
            }

            // Stop both of the timers.
            _nullMessageTimer.Change(Timeout.Infinite, Timeout.Infinite);
            _disconnectTimer.Change(Timeout.Infinite, Timeout.Infinite);

            if (abortive)
            {
                // Discards any pending data when disconnecting.
                _socket.LingerState = new LingerOption(true, 0);
            }
            else
            {
                // Finish graceful shutdown by sending FD_CLOSE.
                _socket.Shutdown(SocketShutdown.Send);
            }

            // Close the socket and dispose the 'NetworkStream'.
            _socket.Close();
            _networkStream.Dispose();

            Disconnected?.Invoke(this, abortive);
        }

        private void OnNullMessageTimerTick(object? state)
        {
            // Send a zero-length message to keep the connection alive.
            _ = SendDataAsync(ReadOnlyMemory<byte>.Empty);
        }

        private void OnDisconnectTimerTick(object? state)
        {
            // No message has been received within the timeout period, disconnect the client abortively.
            SetToDisconnected(true);
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

            _nullMessageTimer.Dispose();
            _disconnectTimer.Dispose();

            _socket.Dispose();
            _networkStream.Dispose();
            _sendLock.Dispose();

            _isDisposed = true;
        }
    }
}
