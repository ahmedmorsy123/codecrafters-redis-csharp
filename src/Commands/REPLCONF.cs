using codecrafters_redis.src.Client;
using codecrafters_redis.src.Resp;
using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src.Commands
{
    public class REPLCONF : ICommand
    {
        public string Name => "REPLCONF";
        public bool IsWrite => false;

        public async Task ExecuteAsync(string[] args, ClientSession session)
        {
            if (args[0] == "GETACK")
            {
                await session.SendStringAsync(RespEncoder.EncodeArray(new string[] { "REPLCONF", "ACK", "0" }));
                return;
            }

            await session.SendStringAsync(RespEncoder.EncodeSimpleString("OK"));
        }
    }
}