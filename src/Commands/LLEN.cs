using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src.Commands
{
    public class LLEN : ICommand
    {
        public string Name => "LLEN";
        public Task<string> ExecuteAsync(string[] args)
        {
            if (args.Length != 1)
            {
                return Task.FromResult(RespEncoder.EncodeError("wrong number of arguments for 'LLEN' command"));
            }
            string key = args[0];
            int length;
            try
            {
                length = StoreProvider.Instance.GetListLength(key);
            }
            catch (InvalidOperationException ex)
            {
                return Task.FromResult(RespEncoder.EncodeError(ex.Message));
            }
            return Task.FromResult(RespEncoder.EncodeInteger(length));
        }
    }
}
