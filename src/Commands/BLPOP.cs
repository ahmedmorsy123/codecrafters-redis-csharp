using codecrafters_redis.src.Client;
using codecrafters_redis.src.Resp;
using codecrafters_redis.src.Storage;
using System.Globalization;

namespace codecrafters_redis.src.Commands
{
    public class BLPOP : ICommand
    {
        public string Name => "BLPOP";
        public bool IsWrite => false;

        public async Task ExecuteAsync(string[] args, ClientSession session)
        {
            if (args.Length < 2)
            {
                await session.SendStringAsync(RespEncoder.EncodeError("wrong number of arguments for 'BLPOP' command"));
                return;
            }

            string[] keys = args.Take(args.Length - 1).ToArray();
            string timeoutText = args[^1];

            if (!double.TryParse(timeoutText, NumberStyles.Float, CultureInfo.InvariantCulture, out double timeoutSeconds) || timeoutSeconds < 0)
            {
                await session.SendStringAsync(RespEncoder.EncodeError("timeout is not a float or out of range"));
                return;
            }

            TimeSpan timeout = TimeSpan.FromSeconds(timeoutSeconds);
            try
            {
                (string Key, string Value)? result = await StoreProvider.Instance.BlockingLPopAsync(keys, timeout);
                if (!result.HasValue)
                {
                    await session.SendStringAsync(RespEncoder.EncodeNullArray());
                    return;
                }

                await session.SendStringAsync(RespEncoder.EncodeArray(new List<string> { result.Value.Key, result.Value.Value }));
            }
            catch (InvalidOperationException ex)
            {
                await session.SendStringAsync(RespEncoder.EncodeError(ex.Message));
            }
        }
    }
}
