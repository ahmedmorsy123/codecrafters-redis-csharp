using codecrafters_redis.src.Client;
using System.Net.Sockets;
using System.Text;

namespace codecrafters_redis.src.Server;

public sealed class ClientConnectionHandler
{
    private readonly CommandDispatcher _dispatcher;

    public ClientConnectionHandler(CommandDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public async Task HandleAsync(Socket client)
    {
        var buffer = new byte[1024];
        var session = new ClientSession { ClientSocket = client };

        try
        {
            while (true)
            {
                int bytesRead = await client.ReceiveAsync(buffer, SocketFlags.None);
                if (bytesRead == 0)
                {
                    Console.Error.WriteLine("Client disconnected.");
                    return;
                }

                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                string responseText = await _dispatcher.DispatchAsync(message, session);

                if (!string.IsNullOrEmpty(responseText))
                {
                    byte[] response = Encoding.UTF8.GetBytes(responseText);
                    await client.SendAsync(response, SocketFlags.None);
                }
            }
        }
        finally
        {
            try { client.Shutdown(SocketShutdown.Both); } catch { /* ignore */ }
            client.Dispose();
        }
    }
}