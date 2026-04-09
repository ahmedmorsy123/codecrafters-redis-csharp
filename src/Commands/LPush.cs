using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src.Commands
{
    public class LPush : ICommand
    {
        public string Name => "LPUSH";
        public Task<string> ExecuteAsync(string[] args)
        {
            if (args.Length < 2)
            {
                return Task.FromResult(RespEncoder.EncodeError("wrong number of arguments for 'LPUSH' command"));
            }
            string key = args[0];
            string[] values = args.Skip(1).ToArray();
            int newLength = StoreProvider.Instance.LPush(key, values);
            return Task.FromResult(RespEncoder.EncodeInteger(newLength));
        }
    }
}
