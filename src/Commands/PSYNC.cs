using codecrafters_redis.src.Client;
using codecrafters_redis.src.Resp;
using System.Text;
using System;

namespace codecrafters_redis.src.Commands
{
    public class PSYNC : ICommand
    {
        public string Name => "PSYNC";
        public async Task ExecuteAsync(string[] args, ClientSession session)
        {
            if (session.ClientSocket is not null)
            {
                ServerInfo.Replicas.Register(session.ClientSocket);
            }

            // args[0] is the replication ID, args[1] is the offset
            string fullResync = RespEncoder.EncodeSimpleString("FULLRESYNC " + ServerInfo.ReplId + " 0");
            await session.SendStringAsync(fullResync);

            var rdbBase64 = "UkVESVMwMDEx+glyZWRpcy12ZXIFNy4yLjD6CnJlZGlzLWJpdHPAQPoFY3RpbWXCbQi8ZfoIdXNlZC1tZW3CsMQQAPoIYW9mLWJhc2XAAP/wbjv+wP9aog==";

            // Decode Base64 -> raw RDB bytes
            byte[] rdbRawBytes = Convert.FromBase64String(rdbBase64);
            await session.SendBytesAsync(RespEncoder.EncodeRDBFile(rdbRawBytes));
            return;
        }
    }
}