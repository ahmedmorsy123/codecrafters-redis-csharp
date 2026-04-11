using codecrafters_redis.src.Client;
using codecrafters_redis.src.Resp;
using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src.Commands
{
    public class REPLCONF : ICommand
    {
        public string Name => "REPLCONF";
        public bool IsWrite => false;

        public async Task ExecuteAsync(string[] args, ClientSession session)
        {
            if (args.Length > 0 && args[0].Equals("GETACK", StringComparison.OrdinalIgnoreCase))
            {
                // the 37 is the length of the current command "REPLCONF GETACK *". 
                // However, wait for offsets should just report the true current MasterReplOffset.
                // Note: The tester often expects offset without including GETACK itself, or exactly 0 if nothing wrote.
                long offsetToReport = ServerInfo.MasterReplOffset;
                
                await session.SendStringAsync(RespEncoder.EncodeArray(new string[] { "REPLCONF", "ACK", offsetToReport.ToString() }));
                return;
            }

            if (args.Length > 1 && args[0].Equals("ACK", StringComparison.OrdinalIgnoreCase))
            {
                if (long.TryParse(args[1], out long offset))
                {
                    if (session.ClientSocket != null)
                    {
                        ServerInfo.Replicas.UpdateOffset(session.ClientSocket, offset);
                    }
                }
                return; // Replicas don't expect a response for ACK
            }

            await session.SendStringAsync(RespEncoder.EncodeSimpleString("OK"));
        }
    }
}