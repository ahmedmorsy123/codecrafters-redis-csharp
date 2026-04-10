using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src.RedisValues
{
    public sealed class RedisString : IRedisValue
    {
        public RedisValueType Type => RedisValueType.String;

        public string Value { get; }
        public RedisString(string value) => Value = value;
    }
}
