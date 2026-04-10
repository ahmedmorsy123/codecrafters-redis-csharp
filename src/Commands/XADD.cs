using codecrafters_redis.src.RedisValues;
using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src.Commands
{
    public class XADD : ICommand
    {
        public string Name => "XADD";

        public Task<string> ExecuteAsync(string[] args)
        {

            Console.WriteLine("Test" + string.Join("- ", args));
            if (args.Length < 3 || args.Length % 2 != 0)
                return Task.FromResult(RespEncoder.EncodeError("wrong number of arguments for 'XADD' command"));

            string streamKey = args[0];
            string requestedId = args[1];
            var fields = new Dictionary<string, string>();
            for (int i = 2; i < args.Length; i += 2)
            {
                fields[args[i]] = args[i + 1];
            }

            RedisStream stream = StoreProvider.Instance.GetOrCreate<RedisStream>(streamKey, () => new RedisStream());
            StreamId id = stream.Add(requestedId, fields);

            return Task.FromResult(RespEncoder.EncodeBulkString(id.ToString()));

        }
    }
}
