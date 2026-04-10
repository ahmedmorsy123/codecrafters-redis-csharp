using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src.Commands
{
    public class PING : ICommand
    {

        public string Name => "PING";
        public async Task<string> ExecuteAsync(string[] args, ClientSession session)
        {
            if (args.Length == 0)
            {
                return "+PONG\r\n";
            }
            else
            {
                return "+" + args[0] + "\r\n";
            }
        }
    }
}
