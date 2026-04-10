using codecrafters_redis.src.RedisValues;

namespace codecrafters_redis.src.Commands
{
    public class XREAD : ICommand
    {
        public string Name => "XREAD";
        public Task<string> ExecuteAsync(string[] args)
        {
            if (args.Length != 3 || !args[0].Equals("streams", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(RespEncoder.EncodeError("wrong number of arguments for 'XREAD' command"));
            }

            string key = args[1];
            StreamId after = StreamId.Parse(args[2]);

            RedisStream stream = StoreProvider.Instance.GetOrCreate<RedisStream>(key, () => new RedisStream());
            IReadOnlyList<StreamEntry> entries = stream.Read(after);

            return Task.FromResult(RespEncoder.EncodeXReadSingleStream(key, entries));
        }
    }
}
