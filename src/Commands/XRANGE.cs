using codecrafters_redis.src.Client;
using codecrafters_redis.src.RedisValues;
using codecrafters_redis.src.Resp;
using codecrafters_redis.src.Storage;
using System;
using System.Collections.Generic;

namespace codecrafters_redis.src.Commands
{
    public class XRANGE : ICommand
    {
        public string Name => "XRANGE";

        public Task<string> ExecuteAsync(string[] args, ClientSession session)
        {
            if (args.Length < 3)
                return Task.FromResult(RespEncoder.EncodeError("wrong number of arguments for 'XRANGE' command"));

            string streamKey = args[0];

            // if startid sequence is missing, it is considered as 0
            // if endid sequence is missing, it is considered as +inf
            StreamId startId = args[1] == "-" ? StreamId.Zero : StreamId.Parse(args[1]);
            StreamId endId = args[2] == "+"
                ? new StreamId(long.MaxValue, long.MaxValue)
                : StreamId.Parse(args[2]);

            RedisStream stream = StoreProvider.Instance.GetOrCreate<RedisStream>(streamKey, () => new RedisStream());
            IReadOnlyList<StreamEntry> entries = stream.Range(startId, endId);
            return Task.FromResult(RespEncoder.EncodeStreamEntries(entries));
        }
    }
}
