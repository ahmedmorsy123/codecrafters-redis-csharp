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

        public Task<string> ExecuteAsync(string[] args, ClientSession session)
        {
            return Task.FromResult(RespEncoder.EncodeSimpleString("OK"));
        }
    }
}
