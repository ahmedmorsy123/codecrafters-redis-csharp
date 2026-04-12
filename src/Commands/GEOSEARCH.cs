using codecrafters_redis.src.Client;
using codecrafters_redis.src.RedisValues;
using codecrafters_redis.src.Resp;
using codecrafters_redis.src.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src.Commands
{
    public class GEOSEARCH : ICommand
    {
        public string Name => "GEOSEARCH";
        public bool IsWrite => false;
        public async Task ExecuteAsync(string[] args, ClientSession session)
        {
            if (args.Length != 7)
            {
                await session.SendStringAsync("-ERR wrong number of arguments for 'GEOSEARCH' command\r\n");
                return;
            }

            string key = args[0];
            if (!double.TryParse(args[2], out double longitude) || !double.TryParse(args[3], out double latitude))
            {
                await session.SendStringAsync("-ERR invalid longitude or latitude\r\n");
                return;
            }

            if (!double.TryParse(args[5], out double radius))
            {
                await session.SendStringAsync("-ERR invalid radius\r\n");
                return;
            }

            radius = ConvertToMeters(radius, args[6].ToUpper());

            string unit = args[6].ToUpper();
            if (unit != "M" && unit != "KM" && unit != "MI" && unit != "FT")
            {
                await session.SendStringAsync("-ERR invalid unit\r\n");
                return;
            }

            RedisSortedSet sortedSet = StoreProvider.Instance.GetOrCreate<RedisSortedSet>(key, () => new RedisSortedSet());
            IReadOnlyList<string> results = sortedSet.GeoRadius(longitude, latitude, radius);
            await session.SendStringAsync(RespEncoder.EncodeArray(results));
        }

        private double ConvertToMeters(double radius, string unit)
        {
            return unit switch
            {
                "M" => radius,
                "KM" => radius * 1000,
                "MI" => radius * 1609.34,
                "FT" => radius * 0.3048,
                _ => throw new ArgumentException("Invalid unit")
            };
        }
    }
}
