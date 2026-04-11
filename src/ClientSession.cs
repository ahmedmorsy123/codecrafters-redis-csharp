using codecrafters_redis.src.Commands;

namespace codecrafters_redis.src
{
    public sealed class ClientSession
    {
        public bool InTransaction { get; set; }

        public ClientWatcher Watcher { get; } = new();

        public List<(ICommand Command, string[] Args)> QueuedCommands { get; } = new();

        public void ResetTransaction()
        {
            InTransaction = false;
            QueuedCommands.Clear();
            Watcher.UnwatchAll();
        }
    }
}
