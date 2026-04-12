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

        // This part is for Geospatial 
        public int AddOrUpdate(string member, double latitude, double longitude)
        {
            if (!GeoUtils.IsValidCoordinates(latitude, longitude)) return 0;

            double score = GeoUtils.EncodeGeoCoordinates(latitude, longitude);
            return AddOrUpdate(member, score);
        }

        public (double latitude, double longitude)? GetCoordinates(string member)
        {
            double? score = GetScore(member);
            if (score == null) return null;
            return GeoUtils.DecodeGeoCoordinates((long)score.Value);
        }

        public double GetDistance(string member1, string member2)
        {
            var coords1 = GetCoordinates(member1);
            var coords2 = GetCoordinates(member2);

            return GeoUtils.calculateDistanceUsingHaversine(coords1.Value.latitude, coords1.Value.longitude, coords2.Value.latitude, coords2.Value.longitude);
        }

        public IReadOnlyList<string> GeoRadius(double longitude, double latitude, double radius)
        {
            var centerScore = GeoUtils.EncodeGeoCoordinates(latitude, longitude);
            var result = new List<string>();
            foreach (var kvp in _scoreToMembers)
            {
                foreach (var member in kvp.Value)
                {
                    var memberScore = GetScore(member);
                    if (memberScore != null)
                    {
                        var distance = GeoUtils.calculateDistanceUsingHaversine(latitude, longitude, GeoUtils.DecodeGeoCoordinates((long)memberScore.Value).latitude, GeoUtils.DecodeGeoCoordinates((long)memberScore.Value).longitude);
                        if (distance <= radius)
                        {
                            result.Add(member);
                        }
                    }
                }
            }
            return result;
        }

    }

    public static class GeoUtils
    {
        private const double MIN_LATITUDE = -85.05112878;
        private const double MAX_LATITUDE = 85.05112878;
        private const double MIN_LONGITUDE = -180;
        private const double MAX_LONGITUDE = 180;
        private const double LATITUDE_RANGE = MAX_LATITUDE - MIN_LATITUDE;
        private const double LONGITUDE_RANGE = MAX_LONGITUDE - MIN_LONGITUDE;

        /// <summary>
        /// Decode converts geo code (WGS84) to tuple of (latitude, longitude)
        /// </summary>
        /// <param name="geoCode">The encoded geographic code</param>
        /// <returns>Tuple containing (latitude, longitude)</returns>
        public static (double latitude, double longitude) DecodeGeoCoordinates(long geoCode)
        {
            // Align bits of both latitude and longitude to take even-numbered position
            long y = geoCode >> 1;
            long x = geoCode;

            // Compact bits back to 32-bit ints
            int gridLatitudeNumber = CompactInt64ToInt32(x);
            int gridLongitudeNumber = CompactInt64ToInt32(y);

            return ConvertGridNumbersToCoordinates(gridLatitudeNumber, gridLongitudeNumber);
        }

        public static long EncodeGeoCoordinates(double latitude, double longitude)
        {
            // Normalize to the range 0-2^26
            double normalizedLatitude = Math.Pow(2, 26) * (latitude - MIN_LATITUDE) / LATITUDE_RANGE;
            double normalizedLongitude = Math.Pow(2, 26) * (longitude - MIN_LONGITUDE) / LONGITUDE_RANGE;

            // Truncate to integers
            int normalizedLatitudeInt = (int)normalizedLatitude;
            int normalizedLongitudeInt = (int)normalizedLongitude;

            return Interleave(normalizedLatitudeInt, normalizedLongitudeInt);
        }

        public static bool IsValidCoordinates(double latitude, double longitude)
        {
            return latitude >= MIN_LATITUDE && latitude <= MAX_LATITUDE &&
                   longitude >= MIN_LONGITUDE && longitude <= MAX_LONGITUDE;
        }

        public static double calculateDistanceUsingHaversine(double lat1, double lon1, double lat2, double lon2)
        {
            var R = 6372797.560856 / 1000; // In kilometers
            var dLat = toRadians(lat2 - lat1);
            var dLon = toRadians(lon2 - lon1);
            lat1 = toRadians(lat1);
            lat2 = toRadians(lat2);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Sin(dLon / 2) * Math.Sin(dLon / 2) * Math.Cos(lat1) * Math.Cos(lat2);
            var c = 2 * Math.Asin(Math.Sqrt(a));
            return R * 2 * Math.Asin(Math.Sqrt(a));
        }

        public static double toRadians(double angle)
        {
            return Math.PI * angle / 180.0;
        }


        private static long Interleave(int x, int y)
        {
            long spreadX = SpreadInt32ToInt64(x);
            long spreadY = SpreadInt32ToInt64(y);
            long yShifted = spreadY << 1;
            return spreadX | yShifted;
        }

        private static long SpreadInt32ToInt64(int v)
        {
            long result = v & 0xFFFFFFFF;
            result = (result | (result << 16)) & 0x0000FFFF0000FFFF;
            result = (result | (result << 8)) & 0x00FF00FF00FF00FF;
            result = (result | (result << 4)) & 0x0F0F0F0F0F0F0F0F;
            result = (result | (result << 2)) & 0x3333333333333333;
            result = (result | (result << 1)) & 0x5555555555555555;
            return result;
        }

        /// <summary>
        /// Compact a 64-bit integer with interleaved bits back to a 32-bit integer.
        /// This is the reverse operation of spread_int32_to_int64.
        /// </summary>
        /// <param name="v">The 64-bit integer with interleaved bits</param>
        /// <returns>Compacted 32-bit integer</returns>
        private static int CompactInt64ToInt32(long v)
        {
            v = v & 0x5555555555555555;
            v = (v | (v >> 1)) & 0x3333333333333333;
            v = (v | (v >> 2)) & 0x0F0F0F0F0F0F0F0F;
            v = (v | (v >> 4)) & 0x00FF00FF00FF00FF;
            v = (v | (v >> 8)) & 0x0000FFFF0000FFFF;
            v = (v | (v >> 16)) & 0x00000000FFFFFFFF;
            return (int)v;
        }

        /// <summary>
        /// Convert grid numbers back to geographic coordinates
        /// </summary>
        /// <param name="gridLatitudeNumber">Grid latitude number</param>
        /// <param name="gridLongitudeNumber">Grid longitude number</param>
        /// <returns>Tuple containing (latitude, longitude)</returns>
        private static (double latitude, double longitude) ConvertGridNumbersToCoordinates(int gridLatitudeNumber, int gridLongitudeNumber)
        {
            // Calculate the grid boundaries
            double gridLatitudeMin = MIN_LATITUDE + LATITUDE_RANGE * (gridLatitudeNumber / Math.Pow(2, 26));
            double gridLatitudeMax = MIN_LATITUDE + LATITUDE_RANGE * ((gridLatitudeNumber + 1) / Math.Pow(2, 26));
            double gridLongitudeMin = MIN_LONGITUDE + LONGITUDE_RANGE * (gridLongitudeNumber / Math.Pow(2, 26));
            double gridLongitudeMax = MIN_LONGITUDE + LONGITUDE_RANGE * ((gridLongitudeNumber + 1) / Math.Pow(2, 26));

            // Calculate the center point of the grid cell
            double latitude = (gridLatitudeMin + gridLatitudeMax) / 2;
            double longitude = (gridLongitudeMin + gridLongitudeMax) / 2;

            return (latitude, longitude);
        }
    }
}
