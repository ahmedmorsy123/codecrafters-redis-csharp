using codecrafters_redis.src.Client;
using codecrafters_redis.src.RedisValues;
using codecrafters_redis.src.Resp;
using codecrafters_redis.src.Storage;

namespace codecrafters_redis.src.Commands
{
    public class Get : ICommand
    {
        public string Name => "GET";

        public async Task ExecuteAsync(string[] args, ClientSession session)
        {
            if (args.Length != 1)
            {
                await session.SendStringAsync(RespEncoder.EncodeError("wrong number of arguments for 'GET' command"));
                return;
            }

            try
            {
                RedisString? entry = StoreProvider.Instance.Get<RedisString>(args[0]);
                await session.SendStringAsync(entry is null
                    ? RespEncoder.EncodeNull()
                    : RespEncoder.EncodeBulkString(entry.Value));
            }
            catch (InvalidOperationException ex)
            {
                await session.SendStringAsync(RespEncoder.EncodeError(ex.Message));
            }
        }
    }
}
