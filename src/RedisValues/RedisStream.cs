namespace codecrafters_redis.src.RedisValues
{
    public sealed class RedisStream : IRedisValue
    {
        public RedisValueType Type => RedisValueType.Stream;
        public int Count => _entries.Count;
        public StreamId LastId => _lastId;

        private readonly List<StreamEntry> _entries = new();
        private StreamId _lastId = StreamId.Zero;

        public StreamId Add(string requestedId, Dictionary<string, string> fields)
        {

            // id possible formates are: 
            // 1. "*" (Auto-generate the time and sequence number)
            // 2. "1526919030474-55" (explicit)
            // 3. "1526919030474-*" (Auto-generate only sequence number)

            StreamId id = requestedId switch
            {
                "*" => StreamId.Generate(_lastId), // auto-generate
                _ when requestedId.EndsWith("-*") => CreateAutoSequenceId(requestedId), // auto-generate sequence
                _ => StreamId.Parse(requestedId) // explicit
            };

            if (id <= _lastId || id == StreamId.Zero) return id; // invalid ID (must be > last ID and not zero)


            _lastId = id;
            _entries.Add(new StreamEntry(id, fields));
            return id;
        }

        private StreamId CreateAutoSequenceId(string requestedId)
        {
            long timestamp = long.Parse(requestedId[..^2]);
            long sequence = timestamp == _lastId.Timestamp
                ? _lastId.Sequence + 1
                : 0;

            return new StreamId(timestamp, sequence);
        }

        public IReadOnlyList<StreamEntry> Range(StreamId start, StreamId end)
        {
            // Return entries with start <= id <= end
            return _entries.Where(e => e.Id >= start && e.Id <= end).ToList();
        }

        public IReadOnlyList<StreamEntry> Read(StreamId after, int count = int.MaxValue)
        {
            // Return entries with id > after, up to count
            return _entries.Where(e => e.Id > after).Take(count).ToList();
        }
    }

    public sealed record StreamEntry(StreamId Id, Dictionary<string, string> Fields);
    public readonly struct StreamId : IComparable<StreamId>
    {
        public long Timestamp { get; }   // milliseconds
        public long Sequence { get; }   // tie-breaker

        public static readonly StreamId Zero = new(0, 0);

        public StreamId(long timestamp, long sequence)
        {
            Timestamp = timestamp;
            Sequence = sequence;
        }

        public static StreamId Generate(StreamId last)
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            return now > last.Timestamp
                ? new StreamId(now, 0)
                : new StreamId(last.Timestamp, last.Sequence + 1); // same ms → bump sequence
        }

        public static StreamId Parse(string id)
        {
            // "1526919030474-55"  →  (1526919030474, 55)
            // "1526919030474-*"     →  (1526919030474, 0)   partial ID
            string[] parts = id.Split('-');
            if (parts.Length is < 1 or > 2)
                throw new FormatException("Invalid stream ID format.");

            long ts = long.Parse(parts[0]);
            long seq = parts.Length == 1
                ? 0
                : (parts[1] == "*" ? 0 : long.Parse(parts[1]));

            return new StreamId(ts, seq);
        }

        public int CompareTo(StreamId other)
        {
            int cmp = Timestamp.CompareTo(other.Timestamp);
            return cmp != 0 ? cmp : Sequence.CompareTo(other.Sequence);
        }

        public static bool operator <=(StreamId a, StreamId b) => a.CompareTo(b) <= 0;
        public static bool operator >=(StreamId a, StreamId b) => a.CompareTo(b) >= 0;
        public static bool operator >(StreamId a, StreamId b) => a.CompareTo(b) > 0;
        public static bool operator <(StreamId a, StreamId b) => a.CompareTo(b) < 0;
        public static bool operator ==(StreamId a, StreamId b) => a.CompareTo(b) == 0;
        public static bool operator !=(StreamId a, StreamId b) => a.CompareTo(b) != 0;

        public override string ToString() => $"{Timestamp}-{Sequence}";
    }
}
