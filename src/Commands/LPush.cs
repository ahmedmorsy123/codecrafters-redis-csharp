using codecrafters_redis.src.RedisValues;
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
                return Task.FromResult(RespEncoder.EncodeError("wrong number of arguments for 'LPUSH' command"));

            string key = args[0];
            string[] values = args.Skip(1).ToArray();

            try
            {
                RedisList list = StoreProvider.Instance.GetOrCreate<RedisList>(key, () => new RedisList());
                list.LPush(values);
                int lengthAfterPush = list.Count;
                StoreProvider.Instance.NotifyBlpopWaiters(key); // ← unblocks BLPOP clients
                return Task.FromResult(RespEncoder.EncodeInteger(lengthAfterPush));
            }
            catch (InvalidOperationException ex)
            {
                return Task.FromResult(RespEncoder.EncodeError(ex.Message));
            }
        }
    }
}
