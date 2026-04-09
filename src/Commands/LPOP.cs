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
            if (args.Length != 1)
            {
                return Task.FromResult(RespEncoder.EncodeError("wrong number of arguments for 'LPOP' command"));
            }
            string key = args[0];
            string? poppedValue;
            try
            {
                poppedValue = StoreProvider.Instance.LPop(key);
            }
            catch (InvalidOperationException ex)
            {
                return Task.FromResult(RespEncoder.EncodeError(ex.Message));
            }
            if (poppedValue == null)
            {
                return Task.FromResult(RespEncoder.EncodeNull());
            }
            return Task.FromResult(RespEncoder.EncodeBulkString(poppedValue));
        }
    }
}
