using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src.Commands
{
    public class WATCH : ICommand
    {
        public string Name => "WATCH";

        public Task<string> ExecuteAsync(string[] args, ClientSession session)
        {
            // Implementation for WATCH command
            return Task.FromResult(RespEncoder.EncodeSimpleString("OK"));
        }
    }
}
