using codecrafters_redis.src.Client;
using codecrafters_redis.src.Resp;
using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src.Commands
{
    public class WAIT : ICommand
    {
        public string Name => "WAIT";

        public bool IsWrite => false;

        public async Task ExecuteAsync(string[] args, ClientSession session)
        {
            // get min number of replicas and timeout in milliseconds
            if (args.Length != 2 || !int.TryParse(args[0], out int minReplicas) || !int.TryParse(args[1], out int timeoutMs))
            {
                await session.SendStringAsync(RespEncoder.EncodeError("ERR wrong number of arguments for 'WAIT' command"));
                return;
            }

            if (minReplicas == 0)
            {
                await session.SendStringAsync(RespEncoder.EncodeInteger(0));
                return;
            }
            else if (ServerInfo.MasterReplOffset == 0)
            {
                
                    var startTime = DateTime.UtcNow;
                    while (ServerInfo.Replicas.ConnectedCount < minReplicas)
                    {
                        if (timeoutMs > 0 && (DateTime.UtcNow - startTime).TotalMilliseconds >= timeoutMs)
                        {
                            break;
                        }
                        await Task.Delay(100);
                    }
                    
                    await session.SendStringAsync(RespEncoder.EncodeInteger(ServerInfo.Replicas.ConnectedCount));
            }else
            {
                // we should send REPLCONF GETACK * to all replicas and wait for their ACKs
                // and if we receive ACKs from at least minReplicas replicas before timeout, we return the number of replicas that acknowledged
                // if we don't receive ACKs from at least minReplicas replicas before timeout, we return the number of replicas that acknowledged
                _ = ServerInfo.Replicas.PropagateAsync(["REPLCONF", "GETACK", "*"]);

                long targetOffset = ServerInfo.MasterReplOffset;
                var startTime = DateTime.UtcNow;
                int currentAcks = 0;

                while (true)
                {
                    currentAcks = ServerInfo.Replicas.GetReplicasWithOffset(targetOffset);
                    if (currentAcks >= minReplicas)
                    {
                        break;
                    }

                    if (timeoutMs > 0 && (DateTime.UtcNow - startTime).TotalMilliseconds >= timeoutMs)
                    {
                        break;
                    }

                    await Task.Delay(50);
                }

                await session.SendStringAsync(RespEncoder.EncodeInteger(currentAcks));
            }
        }
    }
}

