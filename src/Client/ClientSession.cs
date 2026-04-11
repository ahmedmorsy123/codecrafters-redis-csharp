using codecrafters_redis.src.Commands;
using System.Net.Sockets;

namespace codecrafters_redis.src.Client
{
    public sealed class ClientSession
    {
        public Socket? ClientSocket { get; internal set; }

        public bool InTransaction { get; set; }

        public ClientWatcher Watcher { get; } = new();

        public List<(ICommand Command, string[] Args)> QueuedCommands { get; } = new();

        public void ResetTransaction()
        {
            InTransaction = false;
            QueuedCommands.Clear();
            Watcher.UnwatchAll();
        }
        
        public ValueTask<int> SendBytesAsync(ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken = default)
        {
            if (ClientSocket is null)
                throw new InvalidOperationException("Client socket is not set on the session.");

            return ClientSocket.SendAsync(bytes, SocketFlags.None, cancellationToken);
        }
    }
}
