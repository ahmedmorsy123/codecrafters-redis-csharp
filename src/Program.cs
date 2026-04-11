using codecrafters_redis.src;
using codecrafters_redis.src.Replication;
using codecrafters_redis.src.Server;
using System.Net;
using System.Reflection;

ServerOptionsParser.Apply(args);

if (ServerInfo.ReplicaOfHost is not null && ServerInfo.ReplicaOfPort is not null)
{
    _ = Task.Run(async () =>
    {
        try
        {
            await using var master = new MasterClient();
            await master.ConnectAsync(ServerInfo.ReplicaOfHost, ServerInfo.ReplicaOfPort.Value);
            await master.SendAndReceiveAsync(["PING"]);
            await master.SendAsync(["REPLCONF", "listening-port", ServerInfo.Port.ToString()!]);
            await master.SendAsync(["REPLCONF", "capa", "psync2"]);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to connect to master: {ex.Message}");
        }
    });
}

var handlers = CommandHandlerRegistry.BuildFromAssembly(Assembly.GetExecutingAssembly());
var dispatcher = new CommandDispatcher(handlers);
var clientHandler = new ClientConnectionHandler(dispatcher);

var server = new RedisServer(new IPEndPoint(IPAddress.Any, ServerInfo.Port), clientHandler);
await server.StartAsync();