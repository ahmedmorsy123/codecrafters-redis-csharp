using codecrafters_redis.src.RedisValues;

namespace codecrafters_redis.src.Commands
{
    public class Set : ICommand
    {
        public string Name => "SET";

        public Task<string> ExecuteAsync(string[] args)
        {
            if (args.Length != 2 && args.Length != 4)
                return Task.FromResult(RespEncoder.EncodeError("wrong number of arguments for 'SET' command"));

            string key = args[0];
            string value = args[1];

            if (args.Length == 2)
            {
                StoreProvider.Instance.SetEntry(key, new RedisString(value));
                return Task.FromResult(RespEncoder.EncodeSimpleString("OK"));
            }

            if (!long.TryParse(args[3], out long ttlValue) || ttlValue <= 0)
                return Task.FromResult(RespEncoder.EncodeError("invalid expire time in set"));

            TimeSpan ttl;
            if (args[2].Equals("PX", StringComparison.OrdinalIgnoreCase))
                ttl = TimeSpan.FromMilliseconds(ttlValue);
            else if (args[2].Equals("EX", StringComparison.OrdinalIgnoreCase))
                ttl = TimeSpan.FromSeconds(ttlValue);
            else
                return Task.FromResult(RespEncoder.EncodeError("syntax error"));

            StoreProvider.Instance.SetEntry(key, new RedisString(value), ttl);
            return Task.FromResult(RespEncoder.EncodeSimpleString("OK"));
        }
    }
}
