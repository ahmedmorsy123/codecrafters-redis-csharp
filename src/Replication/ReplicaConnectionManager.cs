using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using codecrafters_redis.src.Resp;

namespace codecrafters_redis.src.Replication;

public sealed class ReplicaConnectionManager
{
    private readonly ConcurrentDictionary<Socket, byte> _replicas = new();

    public void Register(Socket replicaSocket)
    {
        _replicas.TryAdd(replicaSocket, 0);
    }

    public void Unregister(Socket replicaSocket)
    {
        _replicas.TryRemove(replicaSocket, out _);
    }

    public async Task PropagateAsync(IReadOnlyList<string> commandWithArgs, CancellationToken cancellationToken = default)
    {
        if (_replicas.IsEmpty)
            return;

        string payload = RespEncoder.EncodeArray(commandWithArgs);
        byte[] bytes = Encoding.UTF8.GetBytes(payload);

        foreach (var socket in _replicas.Keys)
        {
            try
            {
                await socket.SendAsync(bytes, SocketFlags.None, cancellationToken);
            }
            catch
            {
                Unregister(socket);
                try { socket.Shutdown(SocketShutdown.Both); } catch { /* ignore */ }
                socket.Dispose();
            }
        }
    }
}
