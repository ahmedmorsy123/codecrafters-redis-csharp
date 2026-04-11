using codecrafters_redis.src.Client;
using codecrafters_redis.src.Resp;
using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src.Commands
{
    public class DISCARD : ICommand
    {
        public string Name => "DISCARD";

        public Task<string> ExecuteAsync(string[] args, ClientSession session)
        {
            if (!session.InTransaction)
            {
                return Task.FromResult(RespEncoder.EncodeError("ERR DISCARD without MULTI"));
            }

            session.ResetTransaction();
            return Task.FromResult(RespEncoder.EncodeSimpleString("OK"));
        }
    }
}
