using codecrafters_redis.src.Client;
using codecrafters_redis.src.Resp;
using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src.Commands
{
    public class INFO : ICommand
    {
        public string Name => "INFO";

        public async Task ExecuteAsync(string[] args, ClientSession session)
        {
            // args[0] is the section name

            string infoLine = ServerInfo.GetInfoLine("role");
            string masterReplId = ServerInfo.GetInfoLine("master_replid");
            string masterReplOffset = ServerInfo.GetInfoLine("master_repl_offset");
            await session.SendStringAsync(RespEncoder.EncodeBulkString(infoLine + "\n" + masterReplId + "\n" + masterReplOffset));
        }
    }
}
