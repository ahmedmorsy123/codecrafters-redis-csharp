using codecrafters_redis.src.Resp;
using System;
using System.Net.Sockets;
using System.Text;

namespace codecrafters_redis.src.Replication
{
    public sealed class MasterClient : IAsyncDisposable
    {
        private Socket? _socket;

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

    public async Task<byte[]> ReadBulkBytesAsync(CancellationToken cancellationToken = default)
    {
        if (_socket is null)
            throw new InvalidOperationException("Not connected.");

        // Read "$<len>\r\n"
        var header = new List<byte>();
        var one = new byte[1];
        while (true)
        {
            int n = await _socket.ReceiveAsync(one, SocketFlags.None, cancellationToken);
            if (n == 0)
                throw new SocketException((int)SocketError.ConnectionReset);

            header.Add(one[0]);
            int cnt = header.Count;
            if (cnt >= 2 && header[cnt - 2] == (byte)'\r' && header[cnt - 1] == (byte)'\n')
                break;
        }

        string headerText = Encoding.ASCII.GetString(header.ToArray());
        if (headerText.Length < 4 || headerText[0] != '$')
            return Array.Empty<byte>();

        if (!int.TryParse(headerText.AsSpan(1, headerText.Length - 3), out int len) || len <= 0)
        {
            // Consume trailing CRLF for empty bulk
            await ConsumeExactAsync(2, cancellationToken);
            return Array.Empty<byte>();
        }

        byte[] payload = await ConsumeExactAsync(len, cancellationToken);
        await ConsumeExactAsync(2, cancellationToken); // trailing CRLF
        return payload;
    }

    private async Task<byte[]> ConsumeExactAsync(int len, CancellationToken cancellationToken)
    {
        if (_socket is null)
            throw new InvalidOperationException("Not connected.");

        var buf = new byte[len];
        int offset = 0;
        while (offset < len)
        {
            int n = await _socket.ReceiveAsync(buf.AsMemory(offset, len - offset), SocketFlags.None, cancellationToken);
            if (n == 0)
                throw new SocketException((int)SocketError.ConnectionReset);
            offset += n;
        }

        return buf;
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
