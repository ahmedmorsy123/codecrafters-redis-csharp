using codecrafters_redis.src.Client;
using codecrafters_redis.src.RedisValues;
using codecrafters_redis.src.Resp;
using codecrafters_redis.src.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src.Commands
{
    public class ZSCORE : ICommand
    {
        public string Name => "ZSCORE";

        public bool IsWrite => false;

        public async Task ExecuteAsync(string[] args, ClientSession session)
        {
            if (args.Length != 2)
            {
                await session.SendStringAsync("-ERR wrong number of arguments for 'ZSCORE' command\r\n");
                return;
            }

            string key = args[0];
            string member = args[1];
            RedisSortedSet? redisSortedSet = StoreProvider.Instance.Get<RedisSortedSet>(key);
            if (redisSortedSet == null)
            {
                await session.SendStringAsync(RespEncoder.EncodeNull());
                return;
            }

            double? score = redisSortedSet.GetScore(member);
            if (score == null)
            {
                await session.SendStringAsync(RespEncoder.EncodeNull());
                return;
            }

            await session.SendStringAsync(RespEncoder.EncodeBulkString(score.Value.ToString()));
        }
    }
}
