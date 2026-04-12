using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace codecrafters_redis.src
{
    public static class ServerConfigration
    {
        private static Dictionary<string, string> _configs = new Dictionary<string, string>();

        public static void Set(string key, string value)
        {
            _configs.Add(key, value);
        }

        public static bool TryGet(string key, out string? value)
        {
            
            return _configs.TryGetValue(key, out value);
        }
    }
}
