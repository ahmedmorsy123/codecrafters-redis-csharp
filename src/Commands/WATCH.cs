using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src.Commands
{
    public class WATCH : ICommand
    {
        public string Name => "WATCH";

        public Task<string> ExecuteAsync(string[] args, ClientSession session)
        {
            if (session.InTransaction)
            {
                return Task.FromResult(RespEncoder.EncodeError("ERR WATCH inside MULTI is not allowed"));
            }

            if (args.Length < 1)
            {
                return Task.FromResult(RespEncoder.EncodeError("wrong number of arguments for 'WATCH' command"));
            }

            foreach (string key in args)
            {
                session.Watcher.WatchKey(key);
            }

            return Task.FromResult(RespEncoder.EncodeSimpleString("OK"));
        }
    }
}
