using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src.RedisValues
{
    public interface IRedisValue
    {
        RedisValueType Type { get; }
    }

    public enum RedisValueType
    {
        String,
        List,
        Hash,
        Set,
        SortedSet,
        Stream
    }
    
}
