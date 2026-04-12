using codecrafters_redis.src.Client;
using codecrafters_redis.src.RedisValues;
using codecrafters_redis.src.Resp;
using codecrafters_redis.src.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src.Commands
{
    public class ZRANK : ICommand
    {
        public string Name => "ZRANK";

        public bool IsWrite => false;

        public async Task ExecuteAsync(string[] args, ClientSession session)
        {
            if (args.Length != 2)
            {
                await session.SendStringAsync("-ERR wrong number of arguments for 'ZRANK' command\r\n");
                return;
            }

            string key = args[0];
            string member = args[1];

            RedisSortedSet? sortedSet = StoreProvider.Instance.Get<RedisSortedSet>(key);

            if (sortedSet == null)
            {
                await session.SendStringAsync(RespEncoder.EncodeNull());
                return;
            }

            int rank = sortedSet.GetRank(member);

            if (rank == -1)
            {
                await session.SendStringAsync(RespEncoder.EncodeNull());
            }
            else
            {
                await session.SendStringAsync(RespEncoder.EncodeInteger(rank));
            }
        }
    }
}
