using codecrafters_redis.src.Client;
using codecrafters_redis.src.Resp;
using System.Text;

namespace codecrafters_redis.src.Commands
{
    public class PSYNC : ICommand
    {
        public string Name => "PSYNC";
        public async Task ExecuteAsync(string[] args, ClientSession session)
        {
            // args[0] is the replication ID, args[1] is the offset
            string fullResync = RespEncoder.EncodeSimpleString("FULLRESYNC " + ServerInfo.ReplId + " 0");
            await session.SendStringAsync(fullResync);

            var rdbBytes = "UkVESVMwMDEx+glyZWRpcy12ZXIFNy4yLjD6CnJlZGlzLWJpdHPAQPoFY3RpbWXCbQi8ZfoIdXNlZC1tZW3CsMQQAPoIYW9mLWJhc2XAAP/wbjv+wP9aog==";
            string rdb = RespEncoder.EncodeRDBFile(Encoding.ASCII.GetBytes(rdbBytes));
            await session.SendBytesAsync(Encoding.ASCII.GetBytes(rdb));
            return;
        }
    }
}