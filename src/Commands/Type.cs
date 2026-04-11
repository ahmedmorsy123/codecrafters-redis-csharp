using codecrafters_redis.src.Client;
using codecrafters_redis.src.RedisValues;
using codecrafters_redis.src.Resp;
using codecrafters_redis.src.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src.Commands
{
    public class Type : ICommand
    {
        public string Name => "TYPE";
        public async Task ExecuteAsync(string[] args, ClientSession session)
        {
            if (args.Length != 1)
            {
                await session.SendStringAsync(RespEncoder.EncodeError("wrong number of arguments for 'TYPE' command"));
                return;
            }
            
            string key = args[0];
            var entry = StoreProvider.Instance.GetEntry(key);
            if (entry == null) 
            {
                await session.SendStringAsync(RespEncoder.EncodeSimpleString("none"));
                return;
            }

            string typeName = entry?.Value.Type switch
            {
                RedisValueType.String => "string",
                RedisValueType.List => "list",
                RedisValueType.Stream => "stream",
                _ => "none"
            };
            await session.SendStringAsync(RespEncoder.EncodeSimpleString(typeName));
        }
    }
}
