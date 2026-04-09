using System.Net;
using System.Net.Sockets;
using System.Text;

// You can use print statements as follows for debugging, they'll be visible when running tests.
Console.WriteLine("Logs from your program will appear here!");

// Uncomment the code below to pass the first stage
TcpListener server = new TcpListener(IPAddress.Any, 6379);
server.Start();

// Outer loop keeps the server alive and accepts multiple clients
while (true)
{
    Socket client = await server.AcceptSocketAsync(); // wait for client
    Console.Error.WriteLine("Client connected!");

    // We process each client in a separate background task so the server can 
    // go back to accepting new clients immediately.
    _ = Task.Run(async () =>
    {
        var buffer = new byte[1024]; // Larger buffer to fit full Redis commands

        // Inner loop keeps reading messages from this specific client
        while (true)
        {
            int bytesRead = await client.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);

            if (bytesRead == 0)
            {
                // If 0 bytes are read, it means the client disconnected
                Console.WriteLine("Client disconnected.");
                break; 
            }

            string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            Console.Write($"Received: {message}");

            // Send a response back to unblock the client.
            // Redis protocol requires messages to end with \r\n
            byte[] response = Encoding.UTF8.GetBytes("+PONG\r\n");
            await client.SendAsync(new ArraySegment<byte>(response), SocketFlags.None);
        }
    });
}