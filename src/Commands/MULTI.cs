using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src.Commands
{
    public class MULTI : ICommand
    {
        public string Name => "MULTI";

        public Task<string> ExecuteAsync(string[] args)
        {
            CommandStore.isMULTI = true;
            return Task.FromResult(RespEncoder.EncodeSimpleString("OK"));
        }
    }
}
