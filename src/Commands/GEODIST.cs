using codecrafters_redis.src.Client;
using codecrafters_redis.src.RedisValues;
using codecrafters_redis.src.Resp;
using codecrafters_redis.src.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src.Commands
{
    public class GEODIST : ICommand
    {
        public string Name => "GEODIST";
        public bool IsWrite => false;
        public async Task ExecuteAsync(string[] args, ClientSession session)
        {
            if (args.Length != 3)
            {
                await session.SendStringAsync("-ERR wrong number of arguments for 'GEODIST' command\r\n");
                return;
            }

            string key = args[0];
            string member1 = args[1];
            string member2 = args[2];
            RedisSortedSet sortedSet = StoreProvider.Instance.GetOrCreate<RedisSortedSet>(key, () => new RedisSortedSet());
            double distance = sortedSet.GetDistance(member1, member2);
            await session.SendStringAsync(RespEncoder.EncodeBulkString(distance.ToString()));
        }
    }
}
