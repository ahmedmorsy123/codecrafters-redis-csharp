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
                await session.SendStringAsync(RespEncoder.EncodeError("ERR invalid longitude,latitude pair 180.000000,90.000000"));
                return;
            }
            string member = args[3];

            RedisSortedSet sortedSet = StoreProvider.Instance.GetOrCreate<RedisSortedSet>(key, () => new RedisSortedSet());
            int res = sortedSet.AddOrUpdate(member, latitude, longitude);
            if (res == 0)
            {
                await session.SendStringAsync(RespEncoder.EncodeError("ERR invalid longitude,latitude pair 180.000000,90.000000"));
                return;
            }
            await session.SendStringAsync(RespEncoder.EncodeInteger(res));
        }
    }
}
