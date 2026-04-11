using codecrafters_redis.src.Resp;
using System;
using System.Net.Sockets;
using System.Text;

namespace codecrafters_redis.src.Replication
{
    public sealed class MasterClient : IAsyncDisposable
    {
        private Socket? _socket;

        private readonly List<byte> _rxBuffer = new();

        public bool Connected => _socket is not null && _socket.Connected;

        public async Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default)
        {
            if (_socket is not null)
                throw new InvalidOperationException("Already connected.");

            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            try
            {
                await socket.ConnectAsync(host, port, cancellationToken);
                _socket = socket;
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }

        public async Task SendAsync(IReadOnlyList<string> commandWithArgs, CancellationToken cancellationToken = default)
        {
            if (_socket is null)
                throw new InvalidOperationException("Not connected.");

            string payload = RespEncoder.EncodeArray(commandWithArgs);
            byte[] bytes = Encoding.UTF8.GetBytes(payload);
            await _socket.SendAsync(bytes, SocketFlags.None, cancellationToken);
        }

        public async Task<string> ReceiveAsync(CancellationToken cancellationToken = default)
        {
            if (_socket is null)
                throw new InvalidOperationException("Not connected.");

            var buffer = new byte[4096];
            int bytesRead = await _socket.ReceiveAsync(buffer, SocketFlags.None, cancellationToken);
            if (bytesRead == 0)
                throw new SocketException((int)SocketError.ConnectionReset);

            return Encoding.UTF8.GetString(buffer, 0, bytesRead);
        }

        public Task<string> ReadSimpleStringLineAsync(CancellationToken cancellationToken = default)
        {
            return ReadLineAsync(cancellationToken);
        }

        public async Task<byte[]> ReadBulkBytesAsync(CancellationToken cancellationToken = default)
        {
            if (_socket is null)
                throw new InvalidOperationException("Not connected.");

            string headerText = await ReadLineAsync(cancellationToken);
            if (headerText.Length < 2 || headerText[0] != '$')
                return Array.Empty<byte>();

            if (!int.TryParse(headerText.AsSpan(1), out int len))
            {
                return Array.Empty<byte>();
            }

            if (len < 0)
            {
                // Null bulk string.
                return Array.Empty<byte>();
            }

            if (len == 0)
            {
                // Empty bulk string.
                return Array.Empty<byte>();
            }

            byte[] payload = await ReadExactAsync(len, cancellationToken);
            _ = await ReadExactAsync(2, cancellationToken); // trailing CRLF
            return payload;
        }

        public async Task<IReadOnlyList<string>> ReadArrayAsync(CancellationToken cancellationToken = default)
        {
            string arrayHeader = await ReadLineAsync(cancellationToken); // *<count>
            if (arrayHeader.Length < 2 || arrayHeader[0] != '*')
                throw new InvalidOperationException("Expected RESP array.");

            if (!int.TryParse(arrayHeader.AsSpan(1), out int count) || count < 0)
                throw new InvalidOperationException("Invalid RESP array length.");

            var result = new List<string>(count);
            for (int i = 0; i < count; i++)
            {
                string bulkHeader = await ReadLineAsync(cancellationToken); // $<len>
                if (bulkHeader.Length < 2 || bulkHeader[0] != '$')
                    throw new InvalidOperationException("Expected bulk string in array.");

                if (!int.TryParse(bulkHeader.AsSpan(1), out int bulkLen))
                    throw new InvalidOperationException("Invalid bulk length.");

                if (bulkLen < 0)
                {
                    result.Add(string.Empty);
                    continue;
                }

                byte[] payload = await ReadExactAsync(bulkLen, cancellationToken);
                _ = await ReadExactAsync(2, cancellationToken); // trailing CRLF
                result.Add(Encoding.UTF8.GetString(payload));
            }

            return result;
        }

        private async Task EnsureBufferedAsync(int minBytes, CancellationToken cancellationToken)
        {
            if (_socket is null)
                throw new InvalidOperationException("Not connected.");

            while (_rxBuffer.Count < minBytes)
            {
                var buf = new byte[4096];
                int n = await _socket.ReceiveAsync(buf, SocketFlags.None, cancellationToken);
                if (n == 0)
                    throw new SocketException((int)SocketError.ConnectionReset);
                _rxBuffer.AddRange(buf.AsSpan(0, n).ToArray());
            }
        }

        private async Task<string> ReadLineAsync(CancellationToken cancellationToken)
        {
            if (_socket is null)
                throw new InvalidOperationException("Not connected.");

            while (true)
            {
                for (int i = 0; i + 1 < _rxBuffer.Count; i++)
                {
                    if (_rxBuffer[i] == (byte)'\r' && _rxBuffer[i + 1] == (byte)'\n')
                    {
                        string line = Encoding.ASCII.GetString(_rxBuffer.GetRange(0, i).ToArray());
                        _rxBuffer.RemoveRange(0, i + 2);
                        return line;
                    }
                }

                var buf = new byte[4096];
                int n = await _socket.ReceiveAsync(buf, SocketFlags.None, cancellationToken);
                if (n == 0)
                    throw new SocketException((int)SocketError.ConnectionReset);
                _rxBuffer.AddRange(buf.AsSpan(0, n).ToArray());
            }
        }

        private async Task<byte[]> ReadExactAsync(int len, CancellationToken cancellationToken)
        {
            if (_socket is null)
                throw new InvalidOperationException("Not connected.");

            await EnsureBufferedAsync(len, cancellationToken);
            byte[] result = _rxBuffer.GetRange(0, len).ToArray();
            _rxBuffer.RemoveRange(0, len);
            return result;
        }

        public async Task<string> SendAndReceiveAsync(IReadOnlyList<string> commandWithArgs, CancellationToken cancellationToken = default)
        {
            await SendAsync(commandWithArgs, cancellationToken);
            return await ReceiveAsync(cancellationToken);
        }

        public async ValueTask DisposeAsync()
        {
            var socket = _socket;
            _socket = null;

            if (socket is null)
                return;

            try { socket.Shutdown(SocketShutdown.Both); } catch { /* ignore */ }
            socket.Dispose();
            await Task.CompletedTask;
        }
    }
}
