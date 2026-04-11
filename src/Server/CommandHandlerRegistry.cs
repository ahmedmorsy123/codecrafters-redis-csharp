using System.Reflection;
using codecrafters_redis.src.Commands;

namespace codecrafters_redis.src.Server;

public static class CommandHandlerRegistry
{
    public static Dictionary<string, ICommand> BuildFromAssembly(Assembly assembly)
    {
        var commandHandlers = new Dictionary<string, ICommand>(StringComparer.OrdinalIgnoreCase);

        foreach (var t in assembly.GetTypes()
                     .Where(t => typeof(ICommand).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract))
        {
            var instance = (ICommand)Activator.CreateInstance(t)!;
            commandHandlers[instance.Name] = instance;
        }

        return commandHandlers;
    }
}