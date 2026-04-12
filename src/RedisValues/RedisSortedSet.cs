using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src.RedisValues
{
    public sealed class RedisSortedSet : IRedisValue
    {
        public RedisValueType Type => RedisValueType.SortedSet;

        private readonly Dictionary<string, double> _memberToScore = new(StringComparer.Ordinal);
        private readonly SortedDictionary<double, SortedSet<string>> _scoreToMembers = new();

        public int AddOrUpdate(string member, double score)
        {
            if (_memberToScore.TryGetValue(member, out var oldScore))
            {
                if (oldScore == score) return 0;

                var oldBucket = _scoreToMembers[oldScore];
                oldBucket.Remove(member);
                if (oldBucket.Count == 0) _scoreToMembers.Remove(oldScore);

                if (!_scoreToMembers.TryGetValue(score, out var newBucket))
                {
                    newBucket = new SortedSet<string>(StringComparer.Ordinal);
                    _scoreToMembers[score] = newBucket;
                }

                newBucket.Add(member);
                _memberToScore[member] = score;
                return 0;
            }

            if (!_scoreToMembers.TryGetValue(score, out var bucket))
            {
                bucket = new SortedSet<string>(StringComparer.Ordinal);
                _scoreToMembers[score] = bucket;
            }

            bucket.Add(member);
            _memberToScore[member] = score;
            return 1;
        }
    }
}
