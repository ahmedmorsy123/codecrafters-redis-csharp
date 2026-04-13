using codecrafters_redis.src.Client;
using codecrafters_redis.src.RedisValues;
using codecrafters_redis.src.Resp;
using codecrafters_redis.src.Storage;


namespace codecrafters_redis.src.Commands
{
    public class GEOPOS : ICommand
    {
        public string Name => "GEOPOS";

        public bool IsWrite => false;

        public async Task ExecuteAsync(string[] args, ClientSession session)
        {
            if(args.Length >= 2)
            {
                await session.SendStringAsync("-ERR wrong number of arguments for 'GEOPOS' command\r\n");
                return;
            }

            string key = args[0];
            string[] members = args.Skip(1).ToArray();

            RedisSortedSet sortedSet = StoreProvider.Instance.GetOrCreate<RedisSortedSet>(key, () => new RedisSortedSet());

            List<object?> results = new List<object?>();

            foreach (var member in members)
            {
                var coordinates = sortedSet.GetCoordinates(member);
                if (coordinates == null)
                {
                    results.Add(null);
                }
                else
                {
                    results.Add(new[] { coordinates.Value.longitude.ToString(), coordinates.Value.latitude.ToString() });
                }
            }

            await session.SendStringAsync(RespEncoder.EncodeArray(results));
        }
    }
}
