namespace codecrafters_redis.src.Blocking
{
    public sealed class KeyBlockingCoordinator
    {
        private readonly Dictionary<string, LinkedList<IKeyWaiter>> _keyWaiters = new(StringComparer.Ordinal);
        private readonly object _syncRoot = new();
        private long _waiterSequence;

        public async Task<(bool HasResult, TResult Result)> WaitOnKeysAsync<TResult>(
            string[] keys,
            TimeSpan timeout,
            Func<string[], (bool HasResult, TResult Result)> tryServe)
        {
            if (keys.Length == 0)
                throw new ArgumentException("At least one key is required.", nameof(keys));

            string[] orderedKeys = keys.Distinct(StringComparer.Ordinal).ToArray();
            IKeyWaiter waiter;

            lock (_syncRoot)
            {
                (bool HasResult, TResult Result) immediate = tryServe(orderedKeys);
                if (immediate.HasResult)
                {
                    return immediate;
                }

                waiter = RegisterKeyWaiterLocked(
                    orderedKeys,
                    () =>
                    {
                        (bool HasResult, TResult Result) attempt = tryServe(orderedKeys);
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

        public void NotifyKeyChanged(string key)
        {
            lock (_syncRoot)
            {
                FulfillKeyWaitersLocked(key);
            }
        }

        private IKeyWaiter RegisterKeyWaiterLocked(
            string[] keys,
            Func<(bool HasResult, object? Result)> tryServeLocked)
        {
            IKeyWaiter waiter = new KeyWaiter(++_waiterSequence, tryServeLocked);

            foreach (string key in keys)
            {
                if (!_keyWaiters.TryGetValue(key, out LinkedList<IKeyWaiter>? queue))
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
                if (_keyWaiters.TryGetValue(item.Key, out LinkedList<IKeyWaiter>? queue))
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
            if (!_keyWaiters.TryGetValue(key, out LinkedList<IKeyWaiter>? queue)) return;

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
                    if (_keyWaiters.TryGetValue(item.Key, out LinkedList<IKeyWaiter>? otherQueue))
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

        private interface IKeyWaiter
        {
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
                _tryServeLocked = tryServeLocked;
                Completion = new TaskCompletionSource<object?>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
            }

            public bool Completed { get; set; }
            public TaskCompletionSource<object?> Completion { get; }
            public Dictionary<string, LinkedListNode<IKeyWaiter>> NodesByKey { get; }
                = new(StringComparer.Ordinal);

            public (bool HasResult, object? Result) TryServeLocked() => _tryServeLocked();
        }
    }
}
