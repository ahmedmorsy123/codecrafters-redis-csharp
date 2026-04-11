using codecrafters_redis.src.Storage;

namespace codecrafters_redis.src.Client
{
    public sealed class ClientWatcher
    {
        private readonly Dictionary<string, long> _watchedKeyVersions = new(StringComparer.Ordinal);

        public void WatchKey(string key)
        {
            long version = StoreProvider.Instance.GetKeyVersion(key);
            _watchedKeyVersions[key] = version;
        }
        public void UnwatchAll()
        {
            _watchedKeyVersions.Clear();
        }
        public bool IsAnyWatchedKeyModified()
        {
            foreach (var kvp in _watchedKeyVersions)
            {
                long current = StoreProvider.Instance.GetKeyVersion(kvp.Key);
                if (current != kvp.Value)
                    return true;
            }

            return false;
        }
    }
}
