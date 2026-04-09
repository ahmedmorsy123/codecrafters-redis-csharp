using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src.Commands
{
    public class ECHO : ICommand
    {
        public string Name => "ECHO";

        public async Task<string> ExecuteAsync(string[] args)
        {
            if (args.Length != 1)
            {
                return "-ERR wrong number of arguments for 'ECHO' command\r\n";
            }
            else
            {
                return "+" + args[0] + "\r\n";
            }
        }
    }
}
