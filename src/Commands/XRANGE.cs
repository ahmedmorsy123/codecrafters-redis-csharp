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
            StreamId startId = StreamId.Parse(args[1]);
            StreamId endId = StreamId.Parse(args[2]);



            RedisStream stream = StoreProvider.Instance.GetOrCreate<RedisStream>(streamKey, () => new RedisStream());
            IReadOnlyList<StreamEntry> entries = stream.Range(startId, endId);

            List<string> encodedEntries = new();

            foreach (StreamEntry entry in entries)
            {
                string id = entry.Id.ToString();
                List<string> fieldValues = new List<string>();
                foreach (var field in entry.Fields)
                {
                    fieldValues.Add(field.Key);
                    fieldValues.Add(field.Value);
                }

                // Encode the entry as a RESP array
                encodedEntries.Add(RespEncoder.EncodeArray(new[] { id, RespEncoder.EncodeArray(fieldValues.ToArray()) }));
            }

            return Task.FromResult(RespEncoder.EncodeArray(encodedEntries.ToArray()));
        }


        //[
        //[
        //  id,
        //  [
        //    k1,
        //    v1,
        //    k2,
        //    v2
        //  ]
        //]
        //]
    }
}
