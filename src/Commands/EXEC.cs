using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src.Commands
{
    public class EXEC : ICommand
    {
        public string Name => "EXEC";

        public Task<string> ExecuteAsync(string[] args)
        {
            if (CommandStore.isMULTI)
            {
                return CommandStore.ExecuteAllAsync();
            }
            else
            {
                return Task.FromResult(RespEncoder.EncodeError("ERR EXEC without MULTI"));
            }
        }
    }
}
