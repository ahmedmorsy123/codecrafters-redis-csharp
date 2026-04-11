using codecrafters_redis.src.Client;
using codecrafters_redis.src.Resp;
using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src.Commands
{
    public class EXEC : ICommand
    {
        public string Name => "EXEC";
        public bool IsWrite => false;

        public async Task ExecuteAsync(string[] args, ClientSession session)
        {
            if (!session.InTransaction)
            {
                await session.SendStringAsync(RespEncoder.EncodeError("ERR EXEC without MULTI"));
                return;
            }

            if (session.Watcher.IsAnyWatchedKeyModified())
            {
                session.ResetTransaction();
                await session.SendStringAsync(RespEncoder.EncodeNullArray());
                return;
            }

            var queued = session.QueuedCommands.ToArray();
            session.ResetTransaction();

            var result = new StringBuilder();
            result.Append('*').Append(queued.Length).Append("\r\n");
            foreach (var item in queued)
            {
                using var _ = session.CaptureWrites(out var cmdBuf);
                await item.Command.ExecuteAsync(item.Args, session);
                result.Append(cmdBuf);
            }

            await session.SendStringAsync(result.ToString());
        }
    }
}
