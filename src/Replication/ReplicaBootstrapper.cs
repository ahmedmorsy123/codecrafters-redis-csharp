// src/Replication/ReplicaBootstrapper.cs
using System;

namespace codecrafters_redis.src.Replication
{
    public static class ReplicaBootstrapper
    {
        private static readonly Server.CommandDispatcher _dispatcher =
            new(Server.CommandHandlerRegistry.BuildFromAssembly(System.Reflection.Assembly.GetExecutingAssembly()));

        private static readonly Client.ClientSession _session = new();

        public static Task RunAsync(CancellationToken cancellationToken = default)
        {
            return Task.Run(async () =>
            {
                try
                {
                    await using var master = new MasterClient();
                    await SendHandShake(master, cancellationToken);
                    await PumpReplicationStreamAsync(master, cancellationToken);
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
            
            await master.SendAndReceiveAsync(
                ["REPLCONF", "listening-port", ServerInfo.Port.ToString()],
                cancellationToken);

            await master.SendAndReceiveAsync(
                ["REPLCONF", "capa", "psync2"],
                cancellationToken);

            await master.SendAndReceiveAsync(["PSYNC", "?", "-1"], cancellationToken);
        }

        private static async Task PumpReplicationStreamAsync(MasterClient master, CancellationToken cancellationToken)
        {
            // After PSYNC, master sends:
            // 1) +FULLRESYNC ...\r\n
            // 2) $<rdbLen>\r\n<bytes>\r\n
            // Then an endless stream of RESP arrays (propagated commands).

            _ = await master.ReceiveAsync(cancellationToken); // FULLRESYNC line (ignored for now)
            await master.ReadBulkBytesAsync(cancellationToken); // RDB payload (ignored for now)

            while (!cancellationToken.IsCancellationRequested)
            {
                string frame = await master.ReceiveAsync(cancellationToken);
                await ApplyFramesAsync(frame);
            }
        }

        private static async Task ApplyFramesAsync(string respText)
        {
            try
            {
                // Apply as if received from a client, but responses are suppressed by CommandDispatcher on replicas.
                await _dispatcher.DispatchAsync(respText, _session);
            }
            catch
            {
                // ignore malformed/incomplete frames; the simplistic ReceiveAsync can chunk data.
            }
        }
    }
}