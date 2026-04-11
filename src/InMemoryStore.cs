using codecrafters_redis.src.RedisValues;
using codecrafters_redis.src.Blocking;
using System.Collections.Concurrent;

    namespace codecrafters_redis.src
    {
        public sealed class InMemoryStore
        {
            private const string WrongTypeMessage =
                "WRONGTYPE Operation against a key holding the wrong kind of value";

            private readonly ConcurrentDictionary<string, StoreEntry> _entries =
                new(StringComparer.Ordinal);

            private readonly ConcurrentDictionary<string, long> _keyVersions =
                new(StringComparer.Ordinal);

            private readonly KeyBlockingCoordinator _blockingCoordinator = new();

            private readonly object _syncRoot = new();

            // ----------------------------------------------------------------
            // Core generic API
            // ----------------------------------------------------------------

            /// <summary>
            /// Gets the raw store entry for a key, or null if missing/expired.
            /// </summary>
            public StoreEntry? GetEntry(string key)
            {
                lock (_syncRoot)
                {
                    return GetActiveEntry(key);
                }
            }

            /// <summary>
            /// Gets the typed value for a key, creating it if absent.
            /// Throws WRONGTYPE if the key holds a different type.
            /// </summary>
            public T GetOrCreate<T>(string key, Func<T> factory) where T : IRedisValue
        {
                lock (_syncRoot)
                {
                    StoreEntry? entry = GetActiveEntry(key);

                    if (entry is null)
                    {
                        T newValue = factory();
                        _entries[key] = new StoreEntry(newValue, null);
                        BumpKeyVersion(key);
                        _blockingCoordinator.NotifyKeyChanged(key);
                        return newValue;
                    }

                    if (entry.Value is not T typed)
                        throw new InvalidOperationException(WrongTypeMessage);

                    return typed;
                }
            }

            /// <summary>
            /// Gets the typed value for a key — does NOT create if absent.
            /// Returns null if missing. Throws WRONGTYPE if wrong type.
            /// Used by read-only commands (GET, LRANGE, etc.)
            /// </summary>
            public T? Get<T>(string key) where T : class, IRedisValue
            {
                lock (_syncRoot)
                {
                    StoreEntry? entry = GetActiveEntry(key);
                    if (entry is null) return null;

                    if (entry.Value is not T typed)
                        throw new InvalidOperationException(WrongTypeMessage);

                    return typed;
                }
            }

            /// <summary>
            /// Persists the entry back to the store, preserving existing TTL.
            /// Call this after mutating a value obtained via GetOrCreate.
            /// </summary>
            public void SetEntry(string key, IRedisValue value, TimeSpan? ttl = null)
            {
                lock (_syncRoot)
                {
                    DateTimeOffset? expiresAtUtc = ttl.HasValue
                        ? DateTimeOffset.UtcNow.Add(ttl.Value)
                        : GetActiveEntry(key)?.ExpiresAtUtc; // preserve existing TTL

                    _entries[key] = new StoreEntry(value, expiresAtUtc);
                    BumpKeyVersion(key);
                }

                _blockingCoordinator.NotifyKeyChanged(key);
            }

            /// <summary>
            /// Removes a key from the store. Returns true if it existed.
            /// </summary>
            public bool Remove(string key)
            {
                lock (_syncRoot)
                {
                    bool removed = _entries.TryRemove(key, out _);
                    if (removed)
                    {
                        BumpKeyVersion(key);
                        _blockingCoordinator.NotifyKeyChanged(key);
                    }
                    return removed;
                }
            }

            /// <summary>
            /// Sets or updates the TTL on an existing key.
            /// Returns false if the key does not exist.
            /// </summary>
            public bool Expire(string key, TimeSpan ttl)
            {
                lock (_syncRoot)
                {
                    StoreEntry? entry = GetActiveEntry(key);
                    if (entry is null) return false;

                    _entries[key] = new StoreEntry(entry.Value,
                        DateTimeOffset.UtcNow.Add(ttl));
                    BumpKeyVersion(key);
                    _blockingCoordinator.NotifyKeyChanged(key);
                    return true;
                }
            }

            public long GetKeyVersion(string key) =>
                _keyVersions.TryGetValue(key, out long version) ? version : 0;

            // ----------------------------------------------------------------
            // Blocking pop (stays here — needs store-level locking)
            // ----------------------------------------------------------------

            public async Task<(string Key, string Value)?> BlockingLPopAsync(
                string[] keys, TimeSpan timeout)
            {
                (bool HasResult, (string Key, string Value) Result) waitResult =
                    await BlockingWaitOnKeysAsync<(string Key, string Value)>(
                        keys,
                        timeout,
                        TryPopLeftFromKeysLocked);

                return waitResult.HasResult ? waitResult.Result : null;
            }

            public async Task<(bool HasResult, TResult Result)> BlockingWaitOnKeysAsync<TResult>(
                string[] keys,
                TimeSpan timeout,
                Func<string[], (bool HasResult, TResult Result)> tryServeLocked)
            {
                return await _blockingCoordinator.WaitOnKeysAsync(
                    keys,
                    timeout,
                    orderedKeys =>
                    {
                        lock (_syncRoot)
                        {
                            return tryServeLocked(orderedKeys);
                        }
                    });
            }

            /// <summary>
            /// Called by command handlers after an LPush/RPush so blocked
            /// BLPOP clients get notified.
            /// </summary>
            public void NotifyBlpopWaiters(string key)
            {
                NotifyKeyChanged(key);
            }

            public void NotifyKeyChanged(string key)
            {
                _blockingCoordinator.NotifyKeyChanged(key);
            }

            private void BumpKeyVersion(string key)
            {
                _keyVersions.AddOrUpdate(key, 1, (_, current) => current + 1);
            }

            // ----------------------------------------------------------------
            // Private helpers
            // ----------------------------------------------------------------

            private StoreEntry? GetActiveEntry(string key)
            {
                if (!_entries.TryGetValue(key, out StoreEntry? entry))
                    return null;

                if (entry.IsExpired(DateTimeOffset.UtcNow))
                {
                    _entries.TryRemove(key, out _);
                    return null;
                }

                return entry;
            }

            private bool TryPopLeftCore(string key, out string? value)
            {
                value = null;

                StoreEntry? entry = GetActiveEntry(key);
                if (entry is null) return false;

                if (entry.Value is not RedisList list)
                    throw new InvalidOperationException(WrongTypeMessage);

                value = list.LPop().FirstOrDefault();
                if (value is null) return false;

                if (list.Count == 0)
                    _entries.TryRemove(key, out _);

                return true;
            }

            private (bool HasResult, (string Key, string Value) Result) TryPopLeftFromKeysLocked(string[] orderedKeys)
            {
                foreach (string key in orderedKeys)
                {
                    if (TryPopLeftCore(key, out string? value))
                    {
                        return (true, (key, value!));
                    }
                }

                return (false, default);
            }

            public sealed record StoreEntry(IRedisValue Value, DateTimeOffset? ExpiresAtUtc)
            {
                public bool IsExpired(DateTimeOffset nowUtc) =>
                    ExpiresAtUtc.HasValue && nowUtc >= ExpiresAtUtc.Value;
            }
        }

        public static class StoreProvider
        {
            public static InMemoryStore Instance { get; } = new();
        }
    }

