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
        public async Task ExecuteAsync(string[] args, ClientSession session)
        {
            if (args.Length < 2)
            {
                await session.SendStringAsync(RespEncoder.EncodeError("wrong number of arguments for 'RPUSH' command"));
                return;
            }

            string key = args[0];
            string[] values = args.Skip(1).ToArray();

            try
            {
                RedisList list = StoreProvider.Instance.GetOrCreate<RedisList>(key, () => new RedisList());
                list.RPush(values);
                int lengthAfterPush = list.Count;
                StoreProvider.Instance.SetEntry(key, list);
                StoreProvider.Instance.NotifyBlpopWaiters(key); // ← unblocks BLPOP clients
                await session.SendStringAsync(RespEncoder.EncodeInteger(lengthAfterPush));
            }
            catch (InvalidOperationException ex)
            {
                await session.SendStringAsync(RespEncoder.EncodeError(ex.Message));
            }
        }
    }
}
