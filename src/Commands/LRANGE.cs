using codecrafters_redis.src.Client;
using codecrafters_redis.src.RedisValues;
using codecrafters_redis.src.Resp;
using codecrafters_redis.src.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src.Commands
{
    public class LRANGE : ICommand
    {
        public string Name => "LRANGE";
        public bool IsWrite => false;
        public async Task ExecuteAsync(string[] args, ClientSession session)
        {
            if (args.Length != 3)
            {
                await session.SendStringAsync(RespEncoder.EncodeError("wrong number of arguments for 'LRANGE' command"));
                return;
            }

            if (!int.TryParse(args[1], out int start))
            {
                await session.SendStringAsync(RespEncoder.EncodeError("invalid start index"));
                return;
            }

            if (!int.TryParse(args[2], out int stop))
            {
                await session.SendStringAsync(RespEncoder.EncodeError("invalid stop index"));
                return;
            }

            try
            {
                RedisList? list = StoreProvider.Instance.Get<RedisList>(args[0]);
                IReadOnlyList<string> range = list?.Range(start, stop) ?? [];
                await session.SendStringAsync(RespEncoder.EncodeArray(range));
            }
            catch (InvalidOperationException ex)
            {
                await session.SendStringAsync(RespEncoder.EncodeError(ex.Message));
            }
        }
    }
}