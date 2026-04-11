using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using codecrafters_redis.src.Resp;

namespace codecrafters_redis.src.Replication;

public sealed class ReplicaConnectionManager
{
    private readonly ConcurrentDictionary<Socket, long> _replicas = new();

    public int ConnectedCount => _replicas.Count;

    public void Register(Socket replicaSocket)
    {
        _replicas.TryAdd(replicaSocket, 0);
    }

    public void Unregister(Socket replicaSocket)
    {
        _replicas.TryRemove(replicaSocket, out _);
    }

    public void UpdateOffset(Socket replicaSocket, long offset)
    {
        if (_replicas.ContainsKey(replicaSocket))
        {
            _replicas[replicaSocket] = offset;
        }
    }

    public int GetReplicasWithOffset(long targetOffset)
    {
        return _replicas.Values.Count(offset => offset >= targetOffset);
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
                if (!socket.Connected)
                {
                    Unregister(socket);
                    continue;
                }
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
