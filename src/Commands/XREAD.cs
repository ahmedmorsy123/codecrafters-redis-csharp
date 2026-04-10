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

            Dictionary<string, IReadOnlyList<StreamEntry>> streamEntries = new();

            int streamsCount = (args.Length - 1) / 2;
            for (int i = 1; i <= streamsCount; i++)
            {
                string key = args[i];
                StreamId after = StreamId.Parse(args[i + streamsCount]);
                RedisStream stream = StoreProvider.Instance.GetOrCreate<RedisStream>(key, () => new RedisStream());
                IReadOnlyList<StreamEntry> entries = stream.Read(after);
                streamEntries[key] = entries;
            }

            return Task.FromResult(RespEncoder.EncodeXReadMultipleStreams(streamEntries));
        }
    }
}
