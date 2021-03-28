using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Chatter.Application.Internal
{
    internal partial class TcpClient
    {
        public async Task SendDataAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            cancellationToken.ThrowIfCancellationRequested();

            if (Interlocked.CompareExchange(ref _isConnected, 0, 0) == 0)
            {
                throw new InvalidOperationException("The client is not connected.");
            }

            // Wait for any other current send operation.
            await _sendLock.WaitAsync(cancellationToken)
                .ConfigureAwait(false);

            try
            {
                await WriteLengthPrefixAsync(buffer.Length, cancellationToken)
                    .ConfigureAwait(false);

                if (buffer.Length > 0)
                {
                    await _networkStream.WriteAsync(buffer, cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            catch (IOException)
            {
                // The data could not be send, disconnect the client abortively.
                SetToDisconnected(true);

                throw;
            }
            finally
            {
                // Release the lock to allow another send operation.
                _sendLock.Release();
            }
        }

        public Task LoopReceiveDataAsync()
        {
            var pipe = new Pipe();
            var fillTask = FillPipeAsync(pipe.Writer);
            var readTask = ReadPipeAsync(pipe.Reader);

            return Task.WhenAll(fillTask, readTask);
        }

        private async Task FillPipeAsync(PipeWriter writer)
        {
            try
            {
                while (true)
                {
                    // Get a buffer with a predetermined length.
                    var buffer = writer.GetMemory();
                    int bytesRead = await _networkStream.ReadAsync(buffer)
                        .ConfigureAwait(false);

                    if (bytesRead == 0)
                    {
                        // An FD_CLOSE has been received, disconnect the client gracefully.
                        SetToDisconnected(false);

                        break;
                    }

                    // Advance the pipe position by the amount of bytes read.
                    writer.Advance(bytesRead);
                    // Make the data filled into the pipe available to the reader.
                    var result = await writer.FlushAsync()
                        .ConfigureAwait(false);

                    if (result.IsCompleted)
                    {
                        break;
                    }
                }
            }
            catch (IOException)
            {
                // Data could not be received, disconnect the client abortively.
                SetToDisconnected(true);

                throw;
            }
            catch (ObjectDisposedException)
            {
                // We ignore 'ObjectDisposedException' as the 'NetworkStream' has been disposed as part of a disconnection.
            }
            finally
            {
                await writer.CompleteAsync()
                    .ConfigureAwait(false);
            }
        }

        private async Task ReadPipeAsync(PipeReader reader)
        {
            try
            {
                int? length = null;

                while (true)
                {
                    // Read data written to the pipe that has been made available.
                    var result = await reader.ReadAsync()
                        .ConfigureAwait(false);
                    var buffer = result.Buffer;

                    while (true)
                    {
                        if (!length.HasValue)
                        {
                            if (buffer.Length < sizeof(int))
                            {
                                break;
                            }

                            // Enough data is available to read the length prefix.
                            length = ReadLengthPrefix(buffer);
                            // Slice away the part that contains the length prefix.
                            buffer = buffer.Slice(sizeof(int));
                        }

                        if (buffer.Length < length.Value)
                        {
                            break;
                        }

                        // Restart the disconnection timer, because a message has just been received.
                        _disconnectTimer.Change(DisconnectTimeout, Timeout.Infinite);
                        DataReceived?.Invoke(this, buffer.Slice(0, length.Value));

                        // Slice away the bytes that have been processed.
                        buffer = buffer.Slice(length.Value);
                        length = null;
                    }

                    // Mark the processed data as consumed and indicate that the rest has been examined.
                    reader.AdvanceTo(buffer.Start, buffer.End);

                    if (result.IsCompleted)
                    {
                        break;
                    }
                }
            }
            finally
            {
                await reader.CompleteAsync()
                    .ConfigureAwait(false);
            }
        }

        private static int ReadLengthPrefix(ReadOnlySequence<byte> buffer)
        {
            if (buffer.FirstSpan.Length >= sizeof(int))
            {
                return BinaryPrimitives.ReadInt32LittleEndian(buffer.FirstSpan);
            }

            // The first segment of the sequence is too small to contain the length prefix, so we copy the bytes.
            Span<byte> lengthBytes = stackalloc byte[sizeof(int)];
            buffer.Slice(0, sizeof(int))
                .CopyTo(lengthBytes);

            return BinaryPrimitives.ReadInt32LittleEndian(lengthBytes);
        }

        private async Task WriteLengthPrefixAsync(int length, CancellationToken cancellationToken)
        {
            // Rent some bytes to avoid allocation of a new byte array.
            using var memoryOwner = MemoryPool<byte>.Shared.Rent(sizeof(int));

            var lengthBuffer = memoryOwner.Memory;
            BinaryPrimitives.WriteInt32LittleEndian(lengthBuffer.Span, length);

            // Slice away the rest of the buffer, as 'Rent' can return a memory block greater than what was requested.
            await _networkStream.WriteAsync(lengthBuffer.Slice(0, sizeof(int)), cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
