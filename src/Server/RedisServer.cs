using System.Net;
using System.Net.Sockets;

namespace codecrafters_redis.src.Server;

public sealed class RedisServer
{
    private readonly IPEndPoint _endPoint;
    private readonly ClientConnectionHandler _clientHandler;

    public RedisServer(IPEndPoint endPoint, ClientConnectionHandler clientHandler)
    {
        _endPoint = endPoint;
        _clientHandler = clientHandler;
    }

    public async Task StartAsync()
    {
        var listener = new TcpListener(_endPoint);
        listener.Start();

        Console.Error.WriteLine($"Listening on {_endPoint.Address}:{_endPoint.Port}");

        while (true)
        {
            Socket client = await listener.AcceptSocketAsync();
            Console.Error.WriteLine("Client connected!");


            _ = Task.Run(() => _clientHandler.HandleAsync(client));
        }
    }
}