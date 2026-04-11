using codecrafters_redis.src.Client;
using codecrafters_redis.src.RedisValues;
using codecrafters_redis.src.Resp;
using codecrafters_redis.src.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src.Commands
{
    public class INCR : ICommand
    {
        public string Name => "INCR";

        public async Task ExecuteAsync(string[] args, ClientSession session)
        {
            if (args.Length != 1)
            {
                await session.SendStringAsync(RespEncoder.EncodeError("wrong number of arguments for 'INCR' command"));
                return;
            }

            string key = args[0];
            RedisString value = StoreProvider.Instance.GetOrCreate<RedisString>(key, () => new RedisString("0"));

            int intVal;
            if(int.TryParse(value.Value, out intVal))
            {
                intVal++;
                value = new RedisString(intVal.ToString());
                StoreProvider.Instance.SetEntry(key, value);
            }
            else
            {
                await session.SendStringAsync(RespEncoder.EncodeError("ERR value is not an integer or out of range"));
                return;
            }

            if (ServerInfo.Role.Equals("master", StringComparison.OrdinalIgnoreCase))
                await ServerInfo.Replicas.PropagateAsync(new[] { "INCR", key });

            await session.SendStringAsync(RespEncoder.EncodeInteger(intVal));
        }
    }
}