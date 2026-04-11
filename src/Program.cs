using codecrafters_redis.src;
using codecrafters_redis.src.Server;
using System.Net;
using System.Reflection;

ServerOptionsParser.Apply(args);

var handlers = CommandHandlerRegistry.BuildFromAssembly(Assembly.GetExecutingAssembly());
var dispatcher = new CommandDispatcher(handlers);
var clientHandler = new ClientConnectionHandler(dispatcher);

var server = new RedisServer(new IPEndPoint(IPAddress.Any, ServerInfo.Port), clientHandler);
await server.StartAsync();