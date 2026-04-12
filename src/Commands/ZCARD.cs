using codecrafters_redis.src.Client;
using codecrafters_redis.src.RedisValues;
using codecrafters_redis.src.Resp;
using codecrafters_redis.src.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src.Commands
{
    public class ZCARD : ICommand
    {
        public string Name => "ZCARD";

        public bool IsWrite => false;

        public async Task ExecuteAsync(string[] args, ClientSession session)
        {
            if (args.Length != 1)
            {
                await session.SendStringAsync("-ERR wrong number of arguments for 'ZCARD' command\r\n");
                return;
            }

            string key = args[0];
            RedisSortedSet? redisSortedSet = StoreProvider.Instance.Get<RedisSortedSet>(key);
            if (redisSortedSet == null)
            {
                await session.SendStringAsync(RespEncoder.EncodeInteger(0));
                return;
            }

            int count = redisSortedSet.Count;
            await session.SendStringAsync(RespEncoder.EncodeInteger(count));
        }
    }
}
