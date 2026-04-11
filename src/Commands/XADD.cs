using codecrafters_redis.src.Client;
using codecrafters_redis.src.RedisValues;
using codecrafters_redis.src.Resp;
using codecrafters_redis.src.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src.Commands
{
    public class XADD : ICommand
    {
        public string Name => "XADD";
        public bool IsWrite => true;

        public async Task ExecuteAsync(string[] args, ClientSession session)
        {

            if (args.Length < 3 || args.Length % 2 != 0)
            {
                await session.SendStringAsync(RespEncoder.EncodeError("wrong number of arguments for 'XADD' command"));
                return;
            }

            string streamKey = args[0];
            string requestedId = args[1];
            var fields = new Dictionary<string, string>();
            for (int i = 2; i < args.Length; i += 2)
            {
                fields[args[i]] = args[i + 1];
            }

            RedisStream stream = StoreProvider.Instance.GetOrCreate<RedisStream>(streamKey, () => new RedisStream());
            StreamId previousLastId = stream.LastId;
            StreamId id;

            try
            {
                id = stream.Add(requestedId, fields);
            }
            catch (FormatException)
            {
                await session.SendStringAsync(RespEncoder.EncodeError("ERR Invalid stream ID specified as stream command argument"));
                return;
            }
            catch (OverflowException)
            {
                await session.SendStringAsync(RespEncoder.EncodeError("ERR Invalid stream ID specified as stream command argument"));
                return;
            }

            if (id == StreamId.Zero)
            {
                await session.SendStringAsync(RespEncoder.EncodeError("ERR The ID specified in XADD must be greater than 0-0"));
                return;
            }

            if(id <= previousLastId)
            {
                await session.SendStringAsync(RespEncoder.EncodeError("ERR The ID specified in XADD is equal or smaller than the target stream top item"));
                return;
            }

            StoreProvider.Instance.SetEntry(streamKey, stream);
            StoreProvider.Instance.NotifyKeyChanged(streamKey);
            await session.SendStringAsync(RespEncoder.EncodeBulkString(id.ToString()));

        }
    }
}
