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
        public bool IsWrite => false;

        public async Task ExecuteAsync(string[] args, ClientSession session)
        {
            if (!session.InTransaction)
            {
                await session.SendStringAsync(RespEncoder.EncodeError("ERR DISCARD without MULTI"));
                return;
            }

            session.ResetTransaction();
            await session.SendStringAsync(RespEncoder.EncodeSimpleString("OK"));
        }
    }
}
