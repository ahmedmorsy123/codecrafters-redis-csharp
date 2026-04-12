using codecrafters_redis.src.Commands;
using System.Net.Sockets;
using System.Text;

namespace codecrafters_redis.src.Client
{
    public sealed class ClientSession
    {
        public Socket? ClientSocket { get; internal set; }

        public bool IsMasterConnection { get; set; }

        private readonly Stack<StringBuilder?> _captureStack = new();

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

        public ValueTask<int> SendStringAsync(string text, CancellationToken cancellationToken = default)
        {
            if (_captureStack.Count > 0 && _captureStack.Peek() is { } sb)
            {
                sb.Append(text);
                return ValueTask.FromResult(text.Length);
            }

            Console.WriteLine($"text sent is {text}");

            return SendBytesAsync(Encoding.UTF8.GetBytes(text), cancellationToken);
        }

        public IDisposable CaptureWrites(out StringBuilder buffer)
        {
            buffer = new StringBuilder();
            _captureStack.Push(buffer);
            return new CaptureScope(_captureStack);
        }

        private sealed class CaptureScope : IDisposable
        {
            private Stack<StringBuilder?>? _stack;

            public CaptureScope(Stack<StringBuilder?> stack) => _stack = stack;

            public void Dispose()
            {
                var stack = _stack;
                _stack = null;
                if (stack is not null && stack.Count > 0)
                    stack.Pop();
            }
        }
    }
}
