using codecrafters_redis.src.Client;
using codecrafters_redis.src.Resp;
using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src.Commands
{
    public class MULTI : ICommand
    {
        public string Name => "MULTI";
        public bool IsWrite => false;

        public async Task ExecuteAsync(string[] args, ClientSession session)
        {
            if (session.Watcher.IsAnyWatchedKeyModified())
            {
                session.Watcher.UnwatchAll();
                await session.SendStringAsync(RespEncoder.EncodeError("ERR WATCHED key modified"));
                return;
            }

            session.InTransaction = true;
            await session.SendStringAsync(RespEncoder.EncodeSimpleString("OK"));
        }
    }
}
