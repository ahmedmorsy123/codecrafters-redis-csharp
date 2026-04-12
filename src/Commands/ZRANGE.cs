using codecrafters_redis.src.Client;
using codecrafters_redis.src.RedisValues;
using codecrafters_redis.src.Resp;
using codecrafters_redis.src.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src.Commands
{
    public class ZRANGE : ICommand
    {
        public string Name => "ZRANGE";
        public bool IsWrite => false;
        public async Task ExecuteAsync(string[] args, ClientSession session)
        { 
            if(args.Length != 3)
            {
                await session.SendStringAsync("-ERR wrong number of arguments for 'ZRANGE' command\r\n");
                return;
            }
            string key = args[0];
            if (!int.TryParse(args[1], out int start))
            {
                await session.SendStringAsync("-ERR value is not an integer or out of range\r\n");
                return;
            }
            if (!int.TryParse(args[2], out int stop))
            {
                await session.SendStringAsync("-ERR value is not an integer or out of range\r\n");
                return;
            }

            RedisSortedSet? redisSortedSet = StoreProvider.Instance.Get<RedisSortedSet>(key);
            if (redisSortedSet == null)
            {
                await session.SendStringAsync("*0\r\n");
                return;
            }

            IReadOnlyList<string> range = redisSortedSet.Range(start, stop);
            await session.SendStringAsync(RespEncoder.EncodeArray(range));
        }
    }
}
