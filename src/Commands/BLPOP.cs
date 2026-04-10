using System.Globalization;

namespace codecrafters_redis.src.Commands
{
    public class BLPOP : ICommand
    {
        public string Name => "BLPOP";

        public async Task<string> ExecuteAsync(string[] args)
        {
            if (args.Length < 2)
            {
                return RespEncoder.EncodeError("wrong number of arguments for 'BLPOP' command");
            }

            string[] keys = args.Take(args.Length - 1).ToArray();
            string timeoutText = args[^1];

            if (!double.TryParse(timeoutText, NumberStyles.Float, CultureInfo.InvariantCulture, out double timeoutSeconds) || timeoutSeconds < 0)
            {
                return RespEncoder.EncodeError("timeout is not a float or out of range");
            }

            TimeSpan timeout = TimeSpan.FromSeconds(timeoutSeconds);
            try
            {
                (string Key, string Value)? result = await StoreProvider.Instance.BlockingLPopAsync(keys, timeout);
                if (!result.HasValue)
                {
                    return RespEncoder.EncodeNullArray();
                }

                return RespEncoder.EncodeArray(new List<string> { result.Value.Key, result.Value.Value });
            }
            catch (InvalidOperationException ex)
            {
                return RespEncoder.EncodeError(ex.Message);
            }
        }
    }
}
