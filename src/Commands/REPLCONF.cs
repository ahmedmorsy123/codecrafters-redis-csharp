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
                // the 37 is the length of the current command "REPLCONF ACK <offset>". which should not be included.
                await session.SendStringAsync(RespEncoder.EncodeArray(new string[] { "REPLCONF", "ACK", (ServerInfo.MasterReplOffset - 37).ToString() }));
                return;
            }

            await session.SendStringAsync(RespEncoder.EncodeSimpleString("OK"));
        }
    }
}