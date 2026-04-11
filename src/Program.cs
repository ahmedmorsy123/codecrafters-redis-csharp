using codecrafters_redis.src;
using codecrafters_redis.src.Server;
using System.Net;
using System.Reflection;

int port = 6379;
if (args.Length >= 2 && args[0] == "--port" && int.TryParse(args[1], out var parsed))
    port = parsed;

if (port != 6379)
    ServerInfo.role = "replica";

var handlers = CommandHandlerRegistry.BuildFromAssembly(Assembly.GetExecutingAssembly());
var dispatcher = new CommandDispatcher(handlers);
var clientHandler = new ClientConnectionHandler(dispatcher);

var server = new RedisServer(new IPEndPoint(IPAddress.Any, port), clientHandler);
await server.StartAsync();