using codecrafters_redis.src;
using codecrafters_redis.src.Commands;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;

// You can use print statements as follows for debugging, they'll be visible when running tests.
Console.WriteLine("Logs from your program will appear here!");

var commandHandlers = new Dictionary<string, ICommand>(StringComparer.OrdinalIgnoreCase);

Assembly.GetExecutingAssembly().GetTypes()
    .Where(t => typeof(ICommand).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
    .ToList()
    .ForEach(t =>
    {
        var instance = (ICommand)Activator.CreateInstance(t)!;
        commandHandlers[instance.Name] = instance;
    });


TcpListener server = new TcpListener(IPAddress.Any, 6379);
server.Start();
await AcceptClientsAsync(server, commandHandlers);

static async Task AcceptClientsAsync(TcpListener server, Dictionary<string, ICommand> commandHandlers)
{
    // Outer loop keeps the server alive and accepts multiple clients
    while (true)
    {
        Socket client = await server.AcceptSocketAsync(); // wait for client
        Console.Error.WriteLine("Client connected!");

        // We process each client in a separate background task so the server can
        // go back to accepting new clients immediately.
        _ = Task.Run(() => HandleClientAsync(client, commandHandlers));
    }
}

static async Task HandleClientAsync(Socket client, Dictionary<string, ICommand> commandHandlers)
{
    var buffer = new byte[1024]; // Larger buffer to fit full Redis commands
    var session = new ClientSession();

    // Inner loop keeps reading messages from this specific client
    while (true)
    {
        int bytesRead = await client.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);

        if (bytesRead == 0)
        {
            // If 0 bytes are read, it means the client disconnected
            Console.Error.WriteLine("Client disconnected.");
            break;
        }

        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        string responseText = await ProcessRequestAsync(message, commandHandlers, session);

        byte[] response = Encoding.UTF8.GetBytes(responseText);
        await client.SendAsync(new ArraySegment<byte>(response), SocketFlags.None);
    }
}

static async Task<string> ProcessRequestAsync(string message, Dictionary<string, ICommand> commandHandlers, ClientSession session)
{
    List<string> commands = await RespDecoder.DecodeAsync(message);

    Console.Error.WriteLine($"Received: {string.Join(", ", commands)}");

    if (commands.Count == 0)
    {
        return RespEncoder.EncodeError("empty command");
    }

    string commandName = commands[0];
    string[] args = commands.Skip(1).ToArray();

    if (!commandHandlers.TryGetValue(commandName, out var handler))
    {
        return RespEncoder.EncodeError($"unknown command '{commandName}'");
    }

    
    if (session.InTransaction && !commandName.Equals("EXEC", StringComparison.OrdinalIgnoreCase) && !commandName.Equals("DISCARD", StringComparison.OrdinalIgnoreCase) && !commandName.Equals("WATCH",StringComparison.OrdinalIgnoreCase))
    {
        session.QueuedCommands.Add((handler, args));
        return RespEncoder.EncodeSimpleString("QUEUED");
    }

    return await handler.ExecuteAsync(args, session);
}

