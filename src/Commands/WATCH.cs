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
            if (session.InTransaction)
            {
                return Task.FromResult(RespEncoder.EncodeError("ERR WATCH inside MULTI is not allowed"));
            }

            // Implementation for WATCH command
            return Task.FromResult(RespEncoder.EncodeSimpleString("OK"));
        }
    }
}
