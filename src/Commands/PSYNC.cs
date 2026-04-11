using codecrafters_redis.src.Client;
using codecrafters_redis.src.Resp;

namespace codecrafters_redis.src.Commands
{
    public class PSYNC : ICommand
    {
        public string Name => "PSYNC";
        public Task<string> ExecuteAsync(string[] args, ClientSession session)
        {
            // args[0] is the replication ID, args[1] is the offset
            return Task.FromResult(RespEncoder.EncodeSimpleString("FULLRESYNC " + ServerInfo.ReplId + " 0"));
        }
    }
}