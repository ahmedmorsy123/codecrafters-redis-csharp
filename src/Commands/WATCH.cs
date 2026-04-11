using codecrafters_redis.src.Client;
using codecrafters_redis.src.Resp;
using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src.Commands
{
    public class WATCH : ICommand
    {
        public string Name => "WATCH";

        public async Task ExecuteAsync(string[] args, ClientSession session)
        {
            if (session.InTransaction)
            {
                await session.SendStringAsync(RespEncoder.EncodeError("ERR WATCH inside MULTI is not allowed"));
                return;
            }

            if (args.Length < 1)
            {
                await session.SendStringAsync(RespEncoder.EncodeError("wrong number of arguments for 'WATCH' command"));
                return;
            }

            foreach (string key in args)
            {
                session.Watcher.WatchKey(key);
            }

            await session.SendStringAsync(RespEncoder.EncodeSimpleString("OK"));
        }
    }
}
