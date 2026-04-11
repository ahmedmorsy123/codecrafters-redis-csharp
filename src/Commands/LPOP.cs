using codecrafters_redis.src.Client;
using codecrafters_redis.src.RedisValues;
using codecrafters_redis.src.Resp;
using codecrafters_redis.src.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src.Commands
{
    public class LPOP : ICommand
    {
        public string Name => "LPOP";
        public bool IsWrite => true;
        public async Task ExecuteAsync(string[] args, ClientSession session)
        {
            if (args.Length < 1 || args.Length > 2)
            {
                await session.SendStringAsync(RespEncoder.EncodeError("wrong number of arguments for 'LPOP' command"));
                return;
            }

            string key = args[0];
            int count = 1;

            if (args.Length == 2 && (!int.TryParse(args[1], out count) || count <= 0))
            {
                await session.SendStringAsync(RespEncoder.EncodeError("count must be a positive integer"));
                return;
            }

            try
            {
                RedisList? list = StoreProvider.Instance.Get<RedisList>(key);
                if (list is null)
                {
                    await session.SendStringAsync(RespEncoder.EncodeNull());
                    return;
                }

                List<string> popped = list.LPop(count);
                if (popped.Count == 0)
                {
                    await session.SendStringAsync(RespEncoder.EncodeNull());
                    return;
                }

                // Persist mutation so WATCH sees the change via key version bump.
                StoreProvider.Instance.SetEntry(key, list);

                // LPOP key      → single bulk string
                // LPOP key N    → array
                await session.SendStringAsync(args.Length == 1
                    ? RespEncoder.EncodeBulkString(popped[0])
                    : RespEncoder.EncodeArray(popped));
            }
            catch (InvalidOperationException ex)
            {
                await session.SendStringAsync(RespEncoder.EncodeError(ex.Message));
            }
        }
    }
}
