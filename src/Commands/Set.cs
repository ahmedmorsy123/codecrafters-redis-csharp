using codecrafters_redis.src.Client;
using codecrafters_redis.src.RedisValues;
using codecrafters_redis.src.Resp;
using codecrafters_redis.src.Storage;

namespace codecrafters_redis.src.Commands
{
    public class Set : ICommand
    {
        public string Name => "SET";

        public async Task ExecuteAsync(string[] args, ClientSession session)
        {
            if (args.Length != 2 && args.Length != 4)
            {
                await session.SendStringAsync(RespEncoder.EncodeError("wrong number of arguments for 'SET' command"));
                return;
            }

            string key = args[0];
            string value = args[1];

            if (args.Length == 2)
            {
                StoreProvider.Instance.SetEntry(key, new RedisString(value));
                if (ServerInfo.Role.Equals("master", StringComparison.OrdinalIgnoreCase))
                    await ServerInfo.Replicas.PropagateAsync(new[] { "SET", key, value });
                await session.SendStringAsync(RespEncoder.EncodeSimpleString("OK"));
                return;
            }

            if (!long.TryParse(args[3], out long ttlValue) || ttlValue <= 0)
            {
                await session.SendStringAsync(RespEncoder.EncodeError("invalid expire time in set"));
                return;
            }

            TimeSpan ttl;
            if (args[2].Equals("PX", StringComparison.OrdinalIgnoreCase))
                ttl = TimeSpan.FromMilliseconds(ttlValue);
            else if (args[2].Equals("EX", StringComparison.OrdinalIgnoreCase))
                ttl = TimeSpan.FromSeconds(ttlValue);
            else
            {
                await session.SendStringAsync(RespEncoder.EncodeError("syntax error"));
                return;
            }

            StoreProvider.Instance.SetEntry(key, new RedisString(value), ttl);
            if (ServerInfo.Role.Equals("master", StringComparison.OrdinalIgnoreCase))
                await ServerInfo.Replicas.PropagateAsync(new[] { "SET", key, value, args[2], args[3] });
            await session.SendStringAsync(RespEncoder.EncodeSimpleString("OK"));
        }
    }
}
