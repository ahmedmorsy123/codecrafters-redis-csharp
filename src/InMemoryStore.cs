using codecrafters_redis.src.RedisValues;
using System.Collections.Concurrent;

    namespace codecrafters_redis.src
    {
        public sealed class InMemoryStore
        {
            private const string WrongTypeMessage =
                "WRONGTYPE Operation against a key holding the wrong kind of value";

            private readonly ConcurrentDictionary<string, StoreEntry> _entries =
                new(StringComparer.Ordinal);

            private readonly Dictionary<string, LinkedList<IKeyWaiter>> _keyWaiters =
                new(StringComparer.Ordinal);

            private readonly object _syncRoot = new();
            private long _waiterSequence;

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
                }
            }

            /// <summary>
            /// Removes a key from the store. Returns true if it existed.
            /// </summary>
            public bool Remove(string key)
            {
                lock (_syncRoot)
                {
                    return _entries.TryRemove(key, out _);
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
                    return true;
                }
            }

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
                if (keys.Length == 0)
                    throw new ArgumentException("At least one key is required.", nameof(keys));

                string[] orderedKeys = keys.Distinct(StringComparer.Ordinal).ToArray();
                IKeyWaiter waiter;

                lock (_syncRoot)
                {
                    (bool HasResult, TResult Result) immediate = tryServeLocked(orderedKeys);
                    if (immediate.HasResult)
                    {
                        return immediate;
                    }

                    waiter = RegisterKeyWaiterLocked(
                        orderedKeys,
                        () =>
                        {
                            (bool HasResult, TResult Result) attempt = tryServeLocked(orderedKeys);
                            return (attempt.HasResult, (object?)attempt.Result);
                        });
                }

                Task<object?> waitTask = waiter.Completion.Task;

                if (timeout == TimeSpan.Zero)
                {
                    object? result = await waitTask;
                    return (true, (TResult)result!);
                }

                Task completedTask = await Task.WhenAny(waitTask, Task.Delay(timeout));
                if (completedTask == waitTask)
                {
                    object? result = await waitTask;
                    return (true, (TResult)result!);
                }

                lock (_syncRoot)
                {
                    if (!waiter.Completed)
                    {
                        RemoveKeyWaiterLocked(waiter);
                    }
                }

                return (false, default!);
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
                lock (_syncRoot)
                {
                    FulfillKeyWaitersLocked(key);
                }
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

            private IKeyWaiter RegisterKeyWaiterLocked(
                string[] keys,
                Func<(bool HasResult, object? Result)> tryServeLocked)
            {
                IKeyWaiter waiter = new KeyWaiter(++_waiterSequence, tryServeLocked);

                foreach (string key in keys)
                {
                    if (!_keyWaiters.TryGetValue(key,
                        out LinkedList<IKeyWaiter>? queue))
                    {
                        queue = new LinkedList<IKeyWaiter>();
                        _keyWaiters[key] = queue;
                    }

                    LinkedListNode<IKeyWaiter> node = queue.AddLast(waiter);
                    waiter.NodesByKey[key] = node;
                }

                return waiter;
            }

            private void RemoveKeyWaiterLocked(IKeyWaiter waiter)
            {
                foreach (var item in waiter.NodesByKey)
                {
                    if (_keyWaiters.TryGetValue(item.Key,
                        out LinkedList<IKeyWaiter>? queue))
                    {
                        queue.Remove(item.Value);
                        if (queue.Count == 0)
                            _keyWaiters.Remove(item.Key);
                    }
                }

                waiter.NodesByKey.Clear();
                waiter.Completed = true;
            }

            private void FulfillKeyWaitersLocked(string key)
            {
                if (!_keyWaiters.TryGetValue(key,
                    out LinkedList<IKeyWaiter>? queue)) return;

                int initialCount = queue.Count;
                for (int i = 0; i < initialCount && queue.Count > 0; i++)
                {
                    IKeyWaiter waiter = queue.First!.Value;
                    queue.RemoveFirst();
                    waiter.NodesByKey.Remove(key);

                    if (waiter.Completed)
                    {
                        continue;
                    }

                    (bool HasResult, object? Result) attempt = waiter.TryServeLocked();
                    if (!attempt.HasResult)
                    {
                        LinkedListNode<IKeyWaiter> requeueNode = queue.AddLast(waiter);
                        waiter.NodesByKey[key] = requeueNode;
                        continue;
                    }

                    waiter.Completed = true;

                    foreach (var item in waiter.NodesByKey)
                    {
                        if (_keyWaiters.TryGetValue(item.Key,
                            out LinkedList<IKeyWaiter>? otherQueue))
                        {
                            otherQueue.Remove(item.Value);
                            if (otherQueue.Count == 0)
                                _keyWaiters.Remove(item.Key);
                        }
                    }

                    waiter.NodesByKey.Clear();
                    waiter.Completion.TrySetResult(attempt.Result);
                }

                if (queue.Count == 0)
                    _keyWaiters.Remove(key);
            }

            // ----------------------------------------------------------------
            // Inner types
            // ----------------------------------------------------------------

            private interface IKeyWaiter
            {
                long Sequence { get; }
                bool Completed { get; set; }
                TaskCompletionSource<object?> Completion { get; }
                Dictionary<string, LinkedListNode<IKeyWaiter>> NodesByKey { get; }
                (bool HasResult, object? Result) TryServeLocked();
            }

            private sealed class KeyWaiter : IKeyWaiter
            {
                private readonly Func<(bool HasResult, object? Result)> _tryServeLocked;

                public KeyWaiter(long sequence, Func<(bool HasResult, object? Result)> tryServeLocked)
                {
                    Sequence = sequence;
                    _tryServeLocked = tryServeLocked;
                    Completion = new TaskCompletionSource<object?>(
                        TaskCreationOptions.RunContinuationsAsynchronously);
                }

                public long Sequence { get; }
                public bool Completed { get; set; }
                public TaskCompletionSource<object?> Completion { get; }
                public Dictionary<string, LinkedListNode<IKeyWaiter>> NodesByKey { get; }
                    = new(StringComparer.Ordinal);

                public (bool HasResult, object? Result) TryServeLocked() => _tryServeLocked();
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

