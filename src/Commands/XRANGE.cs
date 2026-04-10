using codecrafters_redis.src.RedisValues;
using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src.Commands
{
    public class XRANGE : ICommand
    {
        public string Name => "XRANGE";

        public Task<string> ExecuteAsync(string[] args)
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

            StringBuilder response = new();
            response.Append('*').Append(entries.Count).Append("\r\n");

            foreach (StreamEntry entry in entries)
            {
                response.Append("*2\r\n");
                response.Append(RespEncoder.EncodeBulkString(entry.Id.ToString()));

                response.Append('*').Append(entry.Fields.Count * 2).Append("\r\n");
                foreach (var field in entry.Fields)
                {
                    response.Append(RespEncoder.EncodeBulkString(field.Key));
                    response.Append(RespEncoder.EncodeBulkString(field.Value));
                }
            }

            return Task.FromResult(response.ToString());
        }
    }
}
