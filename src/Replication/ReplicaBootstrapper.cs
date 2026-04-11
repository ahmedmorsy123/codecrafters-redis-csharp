// src/Replication/ReplicaBootstrapper.cs
using System;

namespace codecrafters_redis.src.Replication
{
    public static class ReplicaBootstrapper
    {
        public static Task RunAsync(CancellationToken cancellationToken = default)
        {
            return Task.Run(async () =>
            {
                try
                {
                    await using var master = new MasterClient();
                    await SendHandShake(master, cancellationToken);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Failed to connect to master: {ex.Message}");
                }
            }, cancellationToken);
        }

        private static async Task SendHandShake(MasterClient master, CancellationToken cancellationToken)
        {
            await master.ConnectAsync(ServerInfo.ReplicaOfHost, ServerInfo.ReplicaOfPort.Value, cancellationToken);
            await master.SendAndReceiveAsync(["PING"], cancellationToken);
            
            await master.SendAsync(
                ["REPLCONF", "listening-port", ServerInfo.Port.ToString()],
                cancellationToken);

            await master.SendAsync(
                ["REPLCONF", "capa", "psync2"],
                cancellationToken);

            await master.SendAsync(["PSYNC", "?", "-1"], cancellationToken);
        }
    }
}