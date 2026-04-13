using codecrafters_redis.src.Client;
using codecrafters_redis.src.Resp;
using codecrafters_redis.src.Security;
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
                case "SETUSER":
                    await SETUSERCommand(args.Skip(1).ToArray(), session);
                    break;
                default:
                    await session.SendStringAsync(RespEncoder.EncodeError($"UnKnown SubCommand {subCommand} for ACL"));
                    break;
            }
        }

        private async Task SETUSERCommand(string[] args, ClientSession session)
        {
            string username = args[0];
            string password = args[1];

            if (args[1].StartsWith(">"))
            {
                password = args[1].Substring(1);
            }

            UsersManager.AddPassword(username, password);

            await session.SendStringAsync(RespEncoder.EncodeSimpleString("OK"));
        }

        private async Task GETUSERCommand(string[] args, ClientSession session)
        {
            string username = args[0];

            await session.SendStringAsync(RespEncoder.EncodeArray(UsersManager.GetUserProperties(username)));
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
