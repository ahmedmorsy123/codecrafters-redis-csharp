using codecrafters_redis.src.Client;
using codecrafters_redis.src.Resp;
using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src.Commands
{
    public class UNWATCH : ICommand
    {
        public string Name => "UNWATCH";

        public async Task ExecuteAsync(string[] args, ClientSession session)
        {
            session.Watcher.UnwatchAll();
            await session.SendStringAsync(RespEncoder.EncodeSimpleString("OK"));
        }
    }
}
