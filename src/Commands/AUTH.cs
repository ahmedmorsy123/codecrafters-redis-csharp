using codecrafters_redis.src.Client;
using codecrafters_redis.src.Resp;
using codecrafters_redis.src.Security;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace codecrafters_redis.src.Commands
{
    public class AUTH : ICommand
    {
        public string Name => "AUTH";

        public bool IsWrite => false;

        public async Task ExecuteAsync(string[] args, ClientSession session)
        {
            if(args.Length != 2)
            {
                await session.SendStringAsync("-ERR wrong number of arguments for 'AUTH' command\r\n");
                return;
            }

            string username = args[0];
            string password = args[1];

            if (!UsersManager.Authenticate(username, password))
            {
                await session.SendStringAsync(RespEncoder.EncodeError("WRONGPASS invalid username-password pair or user is disabled."));
                Console.WriteLine("user wasn't authenticated");
                return;

            }
                Console.WriteLine("user was authenticated");

            session.IsAuthenticated = true;
            session.AuthenticatedUser = username;
            await session.SendStringAsync(RespEncoder.EncodeSimpleString("OK"));
        }
    }
}
