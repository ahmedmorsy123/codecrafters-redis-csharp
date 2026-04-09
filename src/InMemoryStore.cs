using System.Collections.Concurrent;

namespace codecrafters_redis.src
{
    public sealed class InMemoryStore
    {
        private const string WrongTypeMessage = "WRONGTYPE Operation against a key holding the wrong kind of value";

        private readonly ConcurrentDictionary<string, StoreEntry> _entries = new(StringComparer.Ordinal);
        private readonly object _syncRoot = new();

        public void Set(string key, string value, TimeSpan? ttl = null)
        {
            lock (_syncRoot)
            {
                DateTimeOffset? expiresAtUtc = ttl.HasValue
                    ? DateTimeOffset.UtcNow.Add(ttl.Value)
                    : null;

                _entries[key] = new StoreEntry(RedisValue.FromString(value), expiresAtUtc);
            }
        }

        public string? Get(string key)
        {
            lock (_syncRoot)
            {
                StoreEntry? entry = GetActiveEntry(key);
                if (entry is null)
                {
                    return null;
                }

                if (entry.Value.Type != RedisValueType.String)
                {
                    throw new InvalidOperationException(WrongTypeMessage);
                }

                return entry.Value.StringValue;
            }
        }

        public long RPush(string key, params string[] values)
        {
            if (values.Length == 0)
            {
                return 0;
            }
            lock (_syncRoot)
            {
                StoreEntry? entry = GetActiveEntry(key);
                List<string> list;
                DateTimeOffset? expiresAtUtc;
                if (entry is null)
                {
                    list = new List<string>();
                    expiresAtUtc = null;
                }
                else
                {
                    if (entry.Value.Type != RedisValueType.List)
                    {
                        throw new InvalidOperationException(WrongTypeMessage);
                    }
                    list = entry.Value.ListValue!;
                    expiresAtUtc = entry.ExpiresAtUtc;
                }
                list.AddRange(values);
                _entries[key] = new StoreEntry(RedisValue.FromList(list), expiresAtUtc);
                return list.Count;
            }
        }

        public long LPush(string key, params string[] values)
        {
            if (values.Length == 0)
            {
                return 0;
            }

            lock (_syncRoot)
            {
                StoreEntry? entry = GetActiveEntry(key);

                List<string> list;
                DateTimeOffset? expiresAtUtc;

                if (entry is null)
                {
                    list = new List<string>();
                    expiresAtUtc = null;
                }
                else
                {
                    if (entry.Value.Type != RedisValueType.List)
                    {
                        throw new InvalidOperationException(WrongTypeMessage);
                    }

                    list = entry.Value.ListValue!;
                    expiresAtUtc = entry.ExpiresAtUtc;
                }

                foreach (string value in values)
                {
                    list.Insert(0, value);
                }

                _entries[key] = new StoreEntry(RedisValue.FromList(list), expiresAtUtc);
                return list.Count;
            }
        }

        public IReadOnlyList<string>? GetList(string key)
        {
            lock (_syncRoot)
            {
                StoreEntry? entry = GetActiveEntry(key);
                if (entry is null)
                {
                    return null;
                }

                if (entry.Value.Type != RedisValueType.List)
                {
                    throw new InvalidOperationException(WrongTypeMessage);
                }

                return entry.Value.ListValue!.ToList();
            }
        }

        public bool Remove(string key)
        {
            lock (_syncRoot)
            {
                return _entries.TryRemove(key, out _);
            }
        }

        private StoreEntry? GetActiveEntry(string key)
        {
            if (!_entries.TryGetValue(key, out StoreEntry? entry))
            {
                return null;
            }

            if (entry.IsExpired(DateTimeOffset.UtcNow))
            {
                _entries.TryRemove(key, out _);
                return null;
            }

            return entry;
        }

        private sealed class RedisValue
        {
            private RedisValue(RedisValueType type, string? stringValue, List<string>? listValue)
            {
                Type = type;
                StringValue = stringValue;
                ListValue = listValue;
            }

            public RedisValueType Type { get; }
            public string? StringValue { get; }
            public List<string>? ListValue { get; }

            public static RedisValue FromString(string value)
            {
                return new RedisValue(RedisValueType.String, value, null);
            }

            public static RedisValue FromList(List<string> value)
            {
                return new RedisValue(RedisValueType.List, null, value);
            }
        }

        private enum RedisValueType
        {
            String,
            List,
        }

        private sealed record StoreEntry(RedisValue Value, DateTimeOffset? ExpiresAtUtc)
        {
            public bool IsExpired(DateTimeOffset nowUtc)
            {
                return ExpiresAtUtc.HasValue && nowUtc >= ExpiresAtUtc.Value;
            }
        }
    }

    public static class StoreProvider
    {
        public static InMemoryStore Instance { get; } = new();
    }
}
