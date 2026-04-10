using codecrafters_redis.src.RedisValues;

namespace codecrafters_redis.src.Commands
{
    public class Get : ICommand
    {
        public string Name => "GET";

        public Task<string> ExecuteAsync(string[] args, ClientSession session)
        {
            if (args.Length != 1)
                return Task.FromResult(RespEncoder.EncodeError("wrong number of arguments for 'GET' command"));

            try
            {
                RedisString? entry = StoreProvider.Instance.Get<RedisString>(args[0]);
                return entry is null
                    ? Task.FromResult(RespEncoder.EncodeNull())
                    : Task.FromResult(RespEncoder.EncodeBulkString(entry.Value));
            }
            catch (InvalidOperationException ex)
            {
                return Task.FromResult(RespEncoder.EncodeError(ex.Message));
            }
        }
    }
}
