using System;

namespace codecrafters_redis.src.Replication
{
    public static class ReplicaBootstrapper
    {
        private static readonly Server.CommandDispatcher _dispatcher =
            new(Server.CommandHandlerRegistry.BuildFromAssembly(System.Reflection.Assembly.GetExecutingAssembly()));

        private static readonly Client.ClientSession _session = new() { IsMasterConnection = true };

        public static Task RunAsync(CancellationToken cancellationToken = default)
        {
            return Task.Run(async () =>
            {
                try
                {
                    await using var master = new MasterClient();
                    await SendHandShake(master, cancellationToken);
                    
                    _session.ClientSocket = master.Socket;
                    
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

            await master.SendAsync(["PING"], cancellationToken);
            _ = await master.ReadSimpleStringLineAsync(cancellationToken);

            await master.SendAsync(["REPLCONF", "listening-port", ServerInfo.Port.ToString()], cancellationToken);
            _ = await master.ReadSimpleStringLineAsync(cancellationToken);

            await master.SendAsync(["REPLCONF", "capa", "psync2"], cancellationToken);
            _ = await master.ReadSimpleStringLineAsync(cancellationToken);

            await master.SendAsync(["PSYNC", "?", "-1"], cancellationToken);
        }

        private static async Task PumpReplicationStreamAsync(MasterClient master, CancellationToken cancellationToken)
        {
            // After PSYNC, master sends:
            // 1) +FULLRESYNC ...\r\n
            // 2) $<rdbLen>\r\n<bytes>\r\n
            // Then an endless stream of RESP arrays (propagated commands).

            _ = await master.ReadSimpleStringLineAsync(cancellationToken); // +FULLRESYNC ...
            await master.ReadBulkBytesAsync(cancellationToken); // Read the RDB payload

            while (!cancellationToken.IsCancellationRequested)
            {
                var cmd = await master.ReadArrayAsync(cancellationToken);
                if (cmd.Count == 0)
                    continue;


                // Re-encode and dispatch through the same pipeline.
                string resp = codecrafters_redis.src.Resp.RespEncoder.EncodeArray(cmd);
                int byteCount = System.Text.Encoding.UTF8.GetByteCount(resp);

                ServerInfo.MasterReplOffset += byteCount;

                await ApplyFramesAsync(resp);
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