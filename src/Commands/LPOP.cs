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
            // first argument is the key, second optional argument is the count
            if (args.Length < 1 || args.Length > 2)
            {
                return Task.FromResult(RespEncoder.EncodeError("wrong number of arguments for 'LPOP' command"));
            }

            string key = args[0];
            int count = 1;
            if (args.Length == 2)
            {
                if (!int.TryParse(args[1], out count) || count <= 0)
                {
                    return Task.FromResult(RespEncoder.EncodeError("count must be a positive integer"));
                }
            }

            List<string> poppedValues;
            try
            {
                poppedValues = StoreProvider.Instance.LPop(key, count);
            }
            catch (InvalidOperationException ex)
            {
                return Task.FromResult(RespEncoder.EncodeError(ex.Message));
            }

            if (poppedValues.Count == 0)
            {
                return Task.FromResult(RespEncoder.EncodeNull());
            }

            if (args.Length == 1)
            {
                return Task.FromResult(RespEncoder.EncodeBulkString(poppedValues[0]));
            }

            return Task.FromResult(RespEncoder.EncodeArray(poppedValues));
        }
    }
}
