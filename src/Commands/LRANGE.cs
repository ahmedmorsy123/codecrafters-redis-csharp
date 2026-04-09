using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src.Commands
{
    public class LRANGE : ICommand
    {
        public string Name => "LRANGE";

        public Task<string> ExecuteAsync(string[] args)
        {
            if (args.Length != 3)
            {
                return Task.FromResult(RespEncoder.EncodeError("wrong number of arguments for 'LRANGE' command"));
            }

            string key = args[0];
            if (!int.TryParse(args[1], out int start))
            {
                return Task.FromResult(RespEncoder.EncodeError("invalid start index"));
            }
            if (!int.TryParse(args[2], out int stop))
            {
                return Task.FromResult(RespEncoder.EncodeError("invalid stop index"));
            }

            IReadOnlyList<string> range = StoreProvider.Instance.GetListRange(key, start, stop);

            return Task.FromResult(RespEncoder.EncodeArray(range));
        }
    }
}