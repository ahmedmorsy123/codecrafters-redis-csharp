using codecrafters_redis.src.Client;
using codecrafters_redis.src.RedisValues;
using codecrafters_redis.src.Resp;
using codecrafters_redis.src.Storage;
using System.Globalization;
using System.Text;


namespace codecrafters_redis.src.Commands
{
    public class GEOPOS : ICommand
    {
        public string Name => "GEOPOS";

        public bool IsWrite => false;

        public async Task ExecuteAsync(string[] args, ClientSession session)
        {
            if(args.Length < 2)
            {
                await session.SendStringAsync("-ERR wrong number of arguments for 'GEOPOS' command\r\n");
                return;
            }

            string key = args[0];
            string[] members = args.Skip(1).ToArray();

            RedisSortedSet? sortedSet = StoreProvider.Instance.Get<RedisSortedSet>(key);

            StringBuilder response = new StringBuilder();
            response.Append('*').Append(members.Length).Append("\r\n");

            foreach (var member in members)
            {
                var coordinates = sortedSet?.GetCoordinates(member);
                if (coordinates == null)
                {
                    response.Append(RespEncoder.EncodeNullArray());
                }
                else
                {
                    response.Append(RespEncoder.EncodeArray(new List<object?>
                    {
                        coordinates.Value.longitude.ToString(CultureInfo.InvariantCulture),
                        coordinates.Value.latitude.ToString(CultureInfo.InvariantCulture)
                    }));
                }
            }

            await session.SendStringAsync(response.ToString());
        }
    }
}
