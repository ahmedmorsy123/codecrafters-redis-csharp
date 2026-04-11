using codecrafters_redis.src.Client;
using codecrafters_redis.src.Resp;
using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src.Commands
{
    public class ECHO : ICommand
    {
        public string Name => "ECHO";
        public bool IsWrite => false;

        public async Task ExecuteAsync(string[] args, ClientSession session)
        {
            if (args.Length != 1)
            {
                await session.SendStringAsync(RespEncoder.EncodeError("wrong number of arguments for 'ECHO' command"));
            }
            else
            {
                await session.SendStringAsync(RespEncoder.EncodeBulkString(args[0]));
            }
        }
    }
}
