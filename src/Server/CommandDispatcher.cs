using codecrafters_redis.src.Client;
using codecrafters_redis.src.Commands;
using codecrafters_redis.src.Resp;

namespace codecrafters_redis.src.Server;

public sealed class CommandDispatcher
{
    private readonly IReadOnlyDictionary<string, ICommand> _handlers;

    public CommandDispatcher(IReadOnlyDictionary<string, ICommand> handlers)
    {
        _handlers = handlers;
    }

    public async Task DispatchAsync(string message, ClientSession session)
    {
        List<string> commands = await RespDecoder.DecodeAsync(message);

        Console.Error.WriteLine($"Received: {string.Join(", ", commands)}");

        if (commands.Count == 0)
        {
            await session.SendStringAsync(RespEncoder.EncodeError("empty command"));
            return;
        }

        string commandName = commands[0];
        string[] args = commands.Skip(1).ToArray();

        if (!_handlers.TryGetValue(commandName, out var handler))
        {
            await session.SendStringAsync(RespEncoder.EncodeError($"unknown command '{commandName}'"));
            return;
        }

        if (session.InTransaction
            && !commandName.Equals("EXEC", StringComparison.OrdinalIgnoreCase)
            && !commandName.Equals("DISCARD", StringComparison.OrdinalIgnoreCase)
            && !commandName.Equals("WATCH", StringComparison.OrdinalIgnoreCase))
        {
            session.QueuedCommands.Add((handler, args));
            await session.SendStringAsync(RespEncoder.EncodeSimpleString("QUEUED"));
            return;
        }

        await handler.ExecuteAsync(args, session);
    }
}