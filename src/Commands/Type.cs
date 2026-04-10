using codecrafters_redis.src.RedisValues;
using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src.Commands
{
    public class Type : ICommand
    {
        public string Name => "TYPE";
        public Task<string> ExecuteAsync(string[] args, ClientSession session)
        {
            if (args.Length != 1)
                return Task.FromResult(RespEncoder.EncodeError("wrong number of arguments for 'TYPE' command"));
            
            string key = args[0];
            var entry = StoreProvider.Instance.GetEntry(key);
            if (entry == null) 
                return Task.FromResult(RespEncoder.EncodeSimpleString("none"));

            string typeName = entry?.Value.Type switch
            {
                RedisValueType.String => "string",
                RedisValueType.List => "list",
                RedisValueType.Stream => "stream",
                _ => "none"
            };
            return Task.FromResult(RespEncoder.EncodeSimpleString(typeName));
        }
    }
}
