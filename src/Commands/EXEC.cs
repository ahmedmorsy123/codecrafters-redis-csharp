using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src.Commands
{
    public class EXEC : ICommand
    {
        public string Name => "EXEC";

        public async Task<string> ExecuteAsync(string[] args, ClientSession session)
        {
            if (!session.InTransaction)
            {
                return RespEncoder.EncodeError("ERR EXEC without MULTI");
            }

            var queued = session.QueuedCommands.ToArray();
            session.ResetTransaction();

            var result = new StringBuilder();
            result.Append('*').Append(queued.Length).Append("\r\n");
            foreach (var item in queued)
            {
                string commandResult = await item.Command.ExecuteAsync(item.Args, session);
                result.Append(commandResult);
            }

            return result.ToString();
        }
    }
}
