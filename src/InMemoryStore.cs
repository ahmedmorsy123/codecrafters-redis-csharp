using System.Collections.Concurrent;

namespace codecrafters_redis.src
{
    public sealed class InMemoryStore
    {
        private readonly ConcurrentDictionary<string, StoreEntry> _entries = new(StringComparer.Ordinal);

        public void Set(string key, string value, TimeSpan? ttl = null)
        {
            DateTimeOffset? expiresAtUtc = ttl.HasValue
                ? DateTimeOffset.UtcNow.Add(ttl.Value)
                : null;

            _entries[key] = new StoreEntry(value, expiresAtUtc);
        }

        public string? Get(string key)
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

            return entry.Value;
        }

        public bool Remove(string key)
        {
            return _entries.TryRemove(key, out _);
        }

        private sealed record StoreEntry(string Value, DateTimeOffset? ExpiresAtUtc)
        {
            public bool IsExpired(DateTimeOffset nowUtc)
            {
                return ExpiresAtUtc.HasValue && nowUtc >= ExpiresAtUtc.Value;
            }
        }
    }

    public static class StoreProvider
    {
        // I used Instance because commands are created via reflection:
        // •	Activator.CreateInstance(t) only works with parameterless constructors.
        // •	So Set/Get(string) can’t receive InMemoryStore through constructor injection in the current design.
        // •	StoreProvider is a simple way to share one store instance across all commands/clients.
        public static InMemoryStore Instance { get; } = new();
    }
}
