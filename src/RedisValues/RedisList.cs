namespace codecrafters_redis.src.RedisValues
{
    public sealed class RedisList : IRedisValue
    {
        public RedisValueType Type => RedisValueType.List;

        private readonly LinkedList<string> _items = new();

        public void LPush(IEnumerable<string> values)
        {
            foreach (string value in values)
            {
                _items.AddFirst(value);
            }
        }

        public void RPush(IEnumerable<string> values)
        {
            foreach(string value in values)
            {
                _items.AddLast(value);
            }
        }
        public List<string> LPop(int count = 1)
        {
            var result = new List<string>(Math.Min(count, _items.Count));
            while (count-- > 0 && _items.Count > 0)
            {
                result.Add(_items.First!.Value);
                _items.RemoveFirst();
            }
            return result;
        }

        public IReadOnlyList<string> Range(int start, int stop)
        {
            // Handle negative indices
            if (start < 0)
            {
                start = Count + start;
            }
            if (stop < 0)
            {
                stop = Count + stop;
            }
            // Adjust indices to be within bounds
            start = Math.Max(0, start);
            stop = Math.Min(Count - 1, stop);
            if (start > stop || start >= Count)
            {
                return new List<string>();
            }

            List<string> result = new();
            LinkedListNode<string>? current = _items.First;
            int index = 0;

            while (current is not null && index < start)
            {
                current = current.Next;
                index++;
            }

            while (current is not null && index <= stop)
            {
                result.Add(current.Value);
                current = current.Next;
                index++;
            }

            return result;
        }
        public int Count => _items.Count;

    }
}
