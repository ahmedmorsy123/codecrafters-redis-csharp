using codecrafters_redis.src.Client;
using codecrafters_redis.src.RedisValues;
using codecrafters_redis.src.Resp;
using codecrafters_redis.src.Storage;

namespace codecrafters_redis.src.Commands
{
    public class XREAD : ICommand
    {
        public string Name => "XREAD";
        public bool IsWrite => false;

        public async Task ExecuteAsync(string[] args, ClientSession session)
        {
            if (args.Length == 0)
            {
                await session.SendStringAsync(RespEncoder.EncodeError("wrong number of arguments for 'XREAD' command"));
                return;
            }

            if (args[0].Equals("streams", StringComparison.OrdinalIgnoreCase))
            {
                await session.SendStringAsync(await ExecuteNonBlockingAsync(args));
                return;
            }

            if (args.Length < 4 || !args[0].Equals("block", StringComparison.OrdinalIgnoreCase))
            {
                await session.SendStringAsync(RespEncoder.EncodeError("wrong number of arguments for 'XREAD' command"));
                return;
            }

            if (!long.TryParse(args[1], out long timeout) || timeout < 0)
            {
                await session.SendStringAsync(RespEncoder.EncodeError("timeout is not an integer or out of range"));
                return;
            }

            await session.SendStringAsync(await ExecuteBlockingAsync(args.Skip(2).ToArray(), timeout));
        }

        private async Task<string> ExecuteBlockingAsync(string[] args, long timeout)
        {
            if (args.Length < 3 || !args[0].Equals("streams", StringComparison.OrdinalIgnoreCase) || ((args.Length - 1) % 2 != 0))
                return RespEncoder.EncodeError("wrong number of arguments for 'XREAD' command");

            int streamsCount = (args.Length - 1) / 2;
            string[] keys = new string[streamsCount];
            StreamId[] afterIds = new StreamId[streamsCount];

            try
            {
                for (int i = 0; i < streamsCount; i++)
                {
                    keys[i] = args[1 + i];
                    string idText = args[1 + streamsCount + i];
                    afterIds[i] = ResolveReadId(keys[i], idText);
                }
            }
            catch (FormatException)
            {
                return RespEncoder.EncodeError("ERR Invalid stream ID specified as stream command argument");
            }
            catch (OverflowException)
            {
                return RespEncoder.EncodeError("ERR Invalid stream ID specified as stream command argument");
            }

            TimeSpan waitTimeout = timeout == 0
                ? TimeSpan.Zero
                : TimeSpan.FromMilliseconds(timeout);

            (bool HasResult, Dictionary<string, IReadOnlyList<StreamEntry>> Result) waitResult =
                await StoreProvider.Instance.BlockingWaitOnKeysAsync(
                    keys,
                    waitTimeout,
                    _ => ReadStreamsSince(keys, afterIds));

            if (!waitResult.HasResult)
                return RespEncoder.EncodeNullArray();

            return RespEncoder.EncodeXReadMultipleStreams(waitResult.Result);
        }

        private Task<string> ExecuteNonBlockingAsync(string[] args)
        {
            if (args.Length < 3 || ((args.Length - 1) % 2 != 0))
                return Task.FromResult(RespEncoder.EncodeError("wrong number of arguments for 'XREAD' command"));

            Dictionary<string, IReadOnlyList<StreamEntry>> streamEntries = new();

            try
            {
                int streamsCount = (args.Length - 1) / 2;
                for (int i = 1; i <= streamsCount; i++)
                {
                    string key = args[i];
                    StreamId after = ResolveReadId(key, args[i + streamsCount]);
                    RedisStream? stream = StoreProvider.Instance.Get<RedisStream>(key);
                    if (stream is null)
                    {
                        continue;
                    }

                    IReadOnlyList<StreamEntry> entries = stream.Read(after);
                    if (entries.Count > 0)
                    {
                        streamEntries[key] = entries;
                    }
                }
            }
            catch (FormatException)
            {
                return Task.FromResult(RespEncoder.EncodeError("ERR Invalid stream ID specified as stream command argument"));
            }
            catch (OverflowException)
            {
                return Task.FromResult(RespEncoder.EncodeError("ERR Invalid stream ID specified as stream command argument"));
            }

            if (streamEntries.Count == 0)
            {
                return Task.FromResult(RespEncoder.EncodeNullArray());
            }

            return Task.FromResult(RespEncoder.EncodeXReadMultipleStreams(streamEntries));
        }

        private (bool HasResult, Dictionary<string, IReadOnlyList<StreamEntry>> Result) ReadStreamsSince(string[] keys, StreamId[] afterIds)
        {
            Dictionary<string, IReadOnlyList<StreamEntry>> streamEntries = new();
            bool any = false;

            for (int i = 0; i < keys.Length; i++)
            {
                RedisStream? stream = StoreProvider.Instance.Get<RedisStream>(keys[i]);
                if (stream is null)
                {
                    continue;
                }

                IReadOnlyList<StreamEntry> entries = stream.Read(afterIds[i]);
                if (entries.Count == 0)
                {
                    continue;
                }

                any = true;
                streamEntries[keys[i]] = entries;
            }

            return (any, streamEntries);
        }

        private StreamId ResolveReadId(string key, string idText)
        {
            if (idText == "$")
            {
                RedisStream? stream = StoreProvider.Instance.Get<RedisStream>(key);
                return stream?.LastId ?? StreamId.Zero;
            }

            return StreamId.Parse(idText);
        }
    }
}
