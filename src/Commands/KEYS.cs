using codecrafters_redis.src.Client;
using codecrafters_redis.src.Resp;
using codecrafters_redis.src.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src.Commands
{
    public class KEYS : ICommand
    {
        public string Name => "KEYS";

        public bool IsWrite => false;

        public async Task ExecuteAsync(string[] args, ClientSession session)
        {
            if (args.Length != 1)
            {
                await session.SendStringAsync("-ERR wrong number of arguments for 'KEYS' command\r\n");
            }

            if (args[0].StartsWith('"') && args[0].EndsWith('"'))
            {
                args[0] = args[0].Substring(1, args[0].Length - 2);
            }

            // For simplicity, we will only support the '*' wildcard at the end of the pattern.
            if (args.Length > 1)
            {
                await session.SendStringAsync("-ERR only one pattern is supported\r\n");
                return;
            }

            if (args.Length > 2)
            {
                await session.SendStringAsync("-ERR too many arguments\r\n");
                return;
            }

            var pattern = args[0];
            var keys = StoreProvider.Instance.GetAllKeys(pattern);

            await session.SendStringAsync(RespEncoder.EncodeArray(keys.ToList()));
        }
    }
}
