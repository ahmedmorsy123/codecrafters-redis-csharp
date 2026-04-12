using codecrafters_redis.src.Client;
using codecrafters_redis.src.RedisValues;
using codecrafters_redis.src.Resp;
using codecrafters_redis.src.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src.Commands
{
    public class ZADD : ICommand
    {
        public string Name => "ZADD";

        public bool IsWrite => true;

        public async Task ExecuteAsync(string[] args, ClientSession session)
        {
            if (args.Length != 3)
            {
                await session.SendStringAsync("-ERR wrong number of arguments for 'ZADD' command\r\n");
                return;
            }

            string key = args[0];
            if (!double.TryParse(args[1], out double score))
            {
                await session.SendStringAsync("-ERR value is not a valid float\r\n");
                return;
            }
            string value = args[2];

            RedisSortedSet sortedSet = StoreProvider.Instance.GetOrCreate<RedisSortedSet>(key, () => new RedisSortedSet());
            int res = sortedSet.AddOrUpdate(value, score);
            await session.SendStringAsync(RespEncoder.EncodeInteger(res));
        }
    }
}
