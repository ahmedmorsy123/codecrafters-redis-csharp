using codecrafters_redis.src.RedisValues;
using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src.Commands
{
    public class LLEN : ICommand
    {
        public string Name => "LLEN";
        public Task<string> ExecuteAsync(string[] args, ClientSession session)
        {
            if (args.Length != 1)
                return Task.FromResult(RespEncoder.EncodeError("wrong number of arguments for 'LLEN' command"));

            try
            {
                RedisList? list = StoreProvider.Instance.Get<RedisList>(args[0]);
                return Task.FromResult(RespEncoder.EncodeInteger(list?.Count ?? 0));
            }
            catch (InvalidOperationException ex)
            {
                return Task.FromResult(RespEncoder.EncodeError(ex.Message));
            }
        }
    }
}
