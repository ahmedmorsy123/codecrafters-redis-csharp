using codecrafters_redis.src.Client;
using codecrafters_redis.src.Persistence;
using codecrafters_redis.src.Resp;
using codecrafters_redis.src.Storage;

namespace codecrafters_redis.src.Commands
{
    public class SAVE : ICommand
    {
        public string Name => "SAVE";
        public bool IsWrite => false;

        public async Task ExecuteAsync(string[] args, ClientSession session)
        {
            if (args.Length != 0)
            {
                await session.SendStringAsync(RespEncoder.EncodeError("wrong number of arguments for 'SAVE' command"));
                return;
            }

            try
            {
                RdbPersistence.SaveToConfiguredFile(StoreProvider.Instance);
                await session.SendStringAsync(RespEncoder.EncodeSimpleString("OK"));
            }
            catch (Exception ex)
            {
                await session.SendStringAsync(RespEncoder.EncodeError($"SAVE failed: {ex.Message}"));
            }
        }
    }
}
