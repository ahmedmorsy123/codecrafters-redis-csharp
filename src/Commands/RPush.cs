using codecrafters_redis.src.Client;
using codecrafters_redis.src.RedisValues;
using codecrafters_redis.src.Resp;
using codecrafters_redis.src.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src.Commands
{
    public class RPush : ICommand
    {
        public string Name => "RPUSH";
        public Task<string> ExecuteAsync(string[] args, ClientSession session)
        {
            if (args.Length < 2)
                return Task.FromResult(RespEncoder.EncodeError("wrong number of arguments for 'RPUSH' command"));

            string key = args[0];
            string[] values = args.Skip(1).ToArray();

            try
            {
                RedisList list = StoreProvider.Instance.GetOrCreate<RedisList>(key, () => new RedisList());
                list.RPush(values);
                int lengthAfterPush = list.Count;
                StoreProvider.Instance.SetEntry(key, list);
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
