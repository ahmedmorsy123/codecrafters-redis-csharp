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

        public Task<string> ExecuteAsync(string[] args, ClientSession session)
        {
            // args[0] is the section name

            string infoLine = ServerInfo.GetInfoLine("role");
            return Task.FromResult(RespEncoder.EncodeBulkString(infoLine));
        }
    }
}
