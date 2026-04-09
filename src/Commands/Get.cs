namespace codecrafters_redis.src.Commands
{
    public class Get : ICommand
    {
        public string Name => "GET";

        public Task<string> ExecuteAsync(string[] args)
        {
            if (args.Length != 1)
            {
                return Task.FromResult(RespEncoder.EncodeError("wrong number of arguments for 'GET' command"));
            }

            string key = args[0];
            string? value = StoreProvider.Instance.Get(key);

            if (value is null)
            {
                return Task.FromResult(RespEncoder.EncodeNull());
            }

            return Task.FromResult(RespEncoder.EncodeBulkString(value));
        }
    }
}
