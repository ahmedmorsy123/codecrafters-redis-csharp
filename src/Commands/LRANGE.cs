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
        public Task<string> ExecuteAsync(string[] args, ClientSession session)
        {
            if (args.Length != 3)
                return Task.FromResult(RespEncoder.EncodeError("wrong number of arguments for 'LRANGE' command"));

            if (!int.TryParse(args[1], out int start))
                return Task.FromResult(RespEncoder.EncodeError("invalid start index"));

            if (!int.TryParse(args[2], out int stop))
                return Task.FromResult(RespEncoder.EncodeError("invalid stop index"));

            try
            {
                RedisList? list = StoreProvider.Instance.Get<RedisList>(args[0]);
                IReadOnlyList<string> range = list?.Range(start, stop) ?? [];
                return Task.FromResult(RespEncoder.EncodeArray(range));
            }
            catch (InvalidOperationException ex)
            {
                return Task.FromResult(RespEncoder.EncodeError(ex.Message));
            }
        }
    }
}