using codecrafters_redis.src.Client;
using codecrafters_redis.src.Resp;
using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src.Commands
{
    public class ACL : ICommand
    {
        public string Name => "ACL";

        public bool IsWrite => false;

        public async Task ExecuteAsync(string[] args, ClientSession session)
        {
            string subCommand = args[0];

            switch (subCommand)
            {
                case "WHOAMI":
                    await WHOAMICommand(args.Skip(1).ToArray(), session);
                    break;
                case "GETUSER":
                    await GETUSERCommand(args.Skip(1).ToArray(), session);
                    break;
                default:
                    await session.SendStringAsync(RespEncoder.EncodeError($"UnKnown SubCommand {subCommand} for ACL"));
                    break;
            }
        }

        private async Task GETUSERCommand(string[] strings, ClientSession session)
        {
            string user = strings[0];

            await session.SendStringAsync(RespEncoder.EncodeArray(new object[] { "flags", new string[] { "nopass" }, "passwords", Array.Empty<string>() }));
        }

        private async Task WHOAMICommand(string[] args, ClientSession session)
        {
            if(args.Length != 0)
            {
                await session.SendStringAsync("-ERR wrong number of arguments for 'ACL' command\r\n");
                return;
            }

            await session.SendStringAsync(RespEncoder.EncodeBulkString("default"));
        }
    }
}
