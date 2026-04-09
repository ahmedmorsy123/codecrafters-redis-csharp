using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src.Commands
{
    public class RPush : ICommand
    {
            public string Name => "RPUSH";
    
            public Task<string> ExecuteAsync(string[] args)
            {
                if (args.Length < 2)
                {
                    return Task.FromResult(RespEncoder.EncodeError("wrong number of arguments for 'RPUSH' command"));
                }
    
                string key = args[0];
                string[] values = args.Skip(1).ToArray();
    
                long newLength = StoreProvider.Instance.RPush(key, values);
                return Task.FromResult(RespEncoder.EncodeInteger((int)newLength));
        }
    }
}
