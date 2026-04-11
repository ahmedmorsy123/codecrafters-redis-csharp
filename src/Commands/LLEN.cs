using codecrafters_redis.src.Client;
using codecrafters_redis.src.RedisValues;
using codecrafters_redis.src.Resp;
using codecrafters_redis.src.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src.Commands
{
    public class LLEN : ICommand
    {
        public string Name => "LLEN";
        public bool IsWrite => false;
        public async Task ExecuteAsync(string[] args, ClientSession session)
        {
            if (args.Length != 1)
            {
                await session.SendStringAsync(RespEncoder.EncodeError("wrong number of arguments for 'LLEN' command"));
                return;
            }

            try
            {
                RedisList? list = StoreProvider.Instance.Get<RedisList>(args[0]);
                await session.SendStringAsync(RespEncoder.EncodeInteger(list?.Count ?? 0));
            }
            catch (InvalidOperationException ex)
            {
                await session.SendStringAsync(RespEncoder.EncodeError(ex.Message));
            }
        }
    }
}
