using codecrafters_redis.src.Channels;
using codecrafters_redis.src.Client;
using codecrafters_redis.src.Resp;
using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src.Commands
{
    public class PUBLISH : ICommand
    {
        public string Name => "PUBLISH";

        public bool IsWrite => true;

        public async Task ExecuteAsync(string[] args, ClientSession session)
        {
            if (args.Length != 2)
            {
                await session.SendStringAsync("-ERR wrong number of arguments for 'PUBLISH' command\r\n");
                return;
            }

            string channel = args[0];
            string message = args[1];
            int subscriberCount = ChannelsManger.GetOrCreateChannel(channel).Publish(message);
            await session.SendStringAsync(RespEncoder.EncodeInteger(subscriberCount));
        }
    }
}
