using codecrafters_redis.src.Client;
using codecrafters_redis.src.Resp;
using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src.Commands
{
    public class PING : ICommand
    {

        public string Name => "PING";
        public bool IsWrite => false;
        public async Task ExecuteAsync(string[] args, ClientSession session)
        {
            Console.WriteLine($"IsSubscribeMode: {session.IsSubscribeMode}");
            if (session.IsSubscribeMode)
            {
                await session.SendStringAsync(RespEncoder.EncodeArray(new string[] { "PONG" , ""}));
            }
            else
            {
                await session.SendStringAsync(RespEncoder.EncodeSimpleString("PONG"));
            }
        }
    }
}
