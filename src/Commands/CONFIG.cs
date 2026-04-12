using codecrafters_redis.src.Client;
using codecrafters_redis.src.Resp;
using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src.Commands
{
    public class CONFIG : ICommand
    {
        public string Name => "CONFIG";

        public bool IsWrite => false;

        public async Task ExecuteAsync(string[] args, ClientSession session)
        {
            if (args.Length < 2)
            {
                await session.SendStringAsync(RespEncoder.EncodeError("ERR wrong number of arguments for 'CONFIG' command"));
                return;
            }

            switch (args[0].ToUpper())
            {
                case "GET":
                    if(ServerConfigration.TryGet(args[1], out var value))
                    {
                        await session.SendStringAsync(RespEncoder.EncodeArray(new[] { args[1], value! }));
                    }
                    else
                    {
                        await session.SendStringAsync(RespEncoder.EncodeArray(Array.Empty<string>()));
                    }
                    await session.SendStringAsync($"*2\r\n$3\r\nmax\r\n$2\r\n10\r\n");
                    break;
                default:
                    await session.SendStringAsync($"-ERR Unsupported CONFIG subcommand '{args[0]}'\r\n");
                    break;
            }
        }
    }
}
