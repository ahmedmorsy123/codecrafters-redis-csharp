using codecrafters_redis.src.RedisValues;
using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src.Commands
{
    public class INCR : ICommand
    {
        public string Name => "INCR";

        public Task<string> ExecuteAsync(string[] args)
        {
            if (args.Length != 1)
                return Task.FromResult(RespEncoder.EncodeError("wrong number of arguments for 'INCR' command"));

            string key = args[0];
            RedisString value = StoreProvider.Instance.GetOrCreate<RedisString>(key, () => new RedisString("1"));

            int intVal;
            if(int.TryParse(value.Value, out intVal))
            {
                intVal++;
                value = new RedisString(intVal.ToString());
                StoreProvider.Instance.SetEntry(key, value);
            }
            else
            {
                // value is not an int
            }


            StoreProvider.Instance.NotifyKeyChanged(key);

            return Task.FromResult(RespEncoder.EncodeInteger(intVal));
        }
    }
}