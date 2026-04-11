using codecrafters_redis.src.Client;
using codecrafters_redis.src.Resp;
using System.Text;

namespace codecrafters_redis.src.Commands
{
    public class PSYNC : ICommand
    {
        public string Name => "PSYNC";
        public async Task<string> ExecuteAsync(string[] args, ClientSession session)
        {
            // args[0] is the replication ID, args[1] is the offset
            string fullResync = RespEncoder.EncodeSimpleString("FULLRESYNC " + ServerInfo.ReplId + " 0");
            await session.SendBytesAsync(Encoding.UTF8.GetBytes(fullResync));

            // Send an (empty) RDB snapshot as a RESP Bulk String: $<len>\r\n<bytes>\r\n
            // Stage 9 expects the master to stream an RDB after FULLRESYNC.
            var rdbBytes = Array.Empty<byte>();

            string rdb = RespEncoder.EncodeRDBFile(rdbBytes);
            await session.SendBytesAsync(Encoding.ASCII.GetBytes(rdb));

            // Response already written directly to the socket.
            return string.Empty;
        }
    }
}