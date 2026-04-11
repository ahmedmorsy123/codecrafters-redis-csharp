using codecrafters_redis.src.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src.Commands
{
    public class PING : ICommand
    {

        public string Name => "PING";
        public async Task ExecuteAsync(string[] args, ClientSession session)
        {
            if (args.Length == 0)
            {
                await session.SendStringAsync("+PONG\r\n");
            }
            else
            {
                await session.SendStringAsync("+" + args[0] + "\r\n");
            }
        }
    }
}
