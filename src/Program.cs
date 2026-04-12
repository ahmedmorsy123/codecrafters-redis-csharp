using codecrafters_redis.src;
using codecrafters_redis.src.Persistence;
using codecrafters_redis.src.Replication;
using codecrafters_redis.src.Server;
using codecrafters_redis.src.Storage;
using System.Net;
using System.Reflection;

ServerOptionsParser.Apply(args);
RdbPersistence.LoadFromConfiguredFile(StoreProvider.Instance);

if (ServerInfo.ReplicaOfHost is not null && ServerInfo.ReplicaOfPort is not null)
{
    _ = ReplicaBootstrapper.RunAsync();
}

var handlers = CommandHandlerRegistry.BuildFromAssembly(Assembly.GetExecutingAssembly());
var dispatcher = new CommandDispatcher(handlers);
var clientHandler = new ClientConnectionHandler(dispatcher);

var server = new RedisServer(new IPEndPoint(IPAddress.Any, ServerInfo.Port), clientHandler);
await server.StartAsync();