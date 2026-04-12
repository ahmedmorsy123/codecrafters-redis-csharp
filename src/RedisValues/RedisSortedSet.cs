using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src.RedisValues
{
    public sealed class RedisSortedSet : IRedisValue
    {
        public RedisValueType Type => RedisValueType.SortedSet;
        public int Count => _memberToScore.Count;

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

        public int GetRank(string member)
        {
            if (!_memberToScore.TryGetValue(member, out var score))
            {
                return -1;
            }

            int rank = 0;
            foreach (var kvp in _scoreToMembers)
            {
                if (kvp.Key == score)
                {
                    foreach (var m in kvp.Value)
                    {
                        if (m == member)
                        {
                            return rank;
                        }
                        rank++;
                    }
                }
                else
                {
                    rank += kvp.Value.Count;
                }
            }

            return -1;
        }

        public IReadOnlyList<string> Range(int start, int stop)
        {
            // start and stop are inclusive, and can be negative to indicate offset from the end
            if (start < 0) start = Count + start;
            if (stop < 0) stop = Count + stop;
            
            if (start < 0) start = 0;
            if (stop >= Count) stop = Count - 1;
            if (start > stop) return new List<string>();

            var result = new List<string>();
            int index = 0;
            foreach (var kvp in _scoreToMembers)
            {
                foreach (var member in kvp.Value)
                {
                    if (index >= start && index <= stop)
                    {
                        result.Add(member);
                    }
                    else if (index > stop)
                    {
                        return result;
                    }
                    index++;
                }
            }
            return result;
        }

        public double? GetScore(string member)
        {
            if (_memberToScore.TryGetValue(member, out var score))
            {
                return score;
            }
            return null;
        }

        public bool Remove(string member)
        {
            if (!_memberToScore.TryGetValue(member, out var score))
            {
                return false;
            }
            var bucket = _scoreToMembers[score];
            bucket.Remove(member);
            if (bucket.Count == 0)
            {
                _scoreToMembers.Remove(score);
            }
            _memberToScore.Remove(member);
            return true;
        }
    }
}
