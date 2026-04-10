using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src.Commands
{
    public class ECHO : ICommand
    {
        public string Name => "ECHO";

        public async Task<string> ExecuteAsync(string[] args, ClientSession session)
        {
            if (args.Length != 1)
            {
                return RespEncoder.EncodeError("wrong number of arguments for 'ECHO' command");
            }
            else
            {
                return RespEncoder.EncodeBulkString(args[0]);
            }
        }
    }
}
