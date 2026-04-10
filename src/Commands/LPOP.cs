using codecrafters_redis.src.RedisValues;
using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src.Commands
{
    public class LPOP : ICommand
    {
        public string Name => "LPOP";
        public Task<string> ExecuteAsync(string[] args)
        {
            if (args.Length < 1 || args.Length > 2)
                return Task.FromResult(RespEncoder.EncodeError("wrong number of arguments for 'LPOP' command"));

            string key = args[0];
            int count = 1;

            if (args.Length == 2 && (!int.TryParse(args[1], out count) || count <= 0))
                return Task.FromResult(RespEncoder.EncodeError("count must be a positive integer"));

            try
            {
                RedisList? list = StoreProvider.Instance.Get<RedisList>(key);
                if (list is null)
                    return Task.FromResult(RespEncoder.EncodeNull());

                List<string> popped = list.LPop(count);
                if (popped.Count == 0)
                    return Task.FromResult(RespEncoder.EncodeNull());

                // LPOP key      → single bulk string
                // LPOP key N    → array
                return args.Length == 1
                    ? Task.FromResult(RespEncoder.EncodeBulkString(popped[0]))
                    : Task.FromResult(RespEncoder.EncodeArray(popped));
            }
            catch (InvalidOperationException ex)
            {
                return Task.FromResult(RespEncoder.EncodeError(ex.Message));
            }
        }
    }
}
