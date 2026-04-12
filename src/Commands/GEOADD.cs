using codecrafters_redis.src.Client;
using codecrafters_redis.src.RedisValues;
using codecrafters_redis.src.Resp;
using codecrafters_redis.src.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src.Commands
{
    public class GEOADD : ICommand
    {
        public string Name => "GEOADD";

        public bool IsWrite => true;

        public async Task ExecuteAsync(string[] args, ClientSession session)
        {
            if(args.Length != 4)
            {
                await session.SendStringAsync("-ERR wrong number of arguments for 'GEOADD' command\r\n");
                return;
            }

            string key = args[0];
            if (!double.TryParse(args[1], out double longitude) || !double.TryParse(args[2], out double latitude))
            {
                await session.SendStringAsync("-ERR invalid longitude or latitude\r\n");
                return;
            }
            string member = args[3];

            RedisSortedSet sortedSet = StoreProvider.Instance.GetOrCreate<RedisSortedSet>(key, () => new RedisSortedSet());
            int res = sortedSet.AddOrUpdate(member, latitude, longitude);
            await session.SendStringAsync(RespEncoder.EncodeInteger(res));
        }
    }
}
