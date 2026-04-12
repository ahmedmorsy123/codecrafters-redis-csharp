using codecrafters_redis.src.Channels;
using codecrafters_redis.src.Client;
using codecrafters_redis.src.Resp;
using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src.Commands
{
    public class UNSUBSCRIBE : ICommand
    {
        public string Name => "UNSUBSCRIBE";

        public bool IsWrite => false;

        public async Task ExecuteAsync(string[] args, ClientSession session)
        {
            if (args.Length != 1)
            {
                await session.SendStringAsync("-ERR wrong number of arguments for 'UNSUBSCRIBE' command\r\n");
                return;
            }

            string channel = args[0];
            var existingChannel = ChannelsManager.GetChannel(channel);
            if (existingChannel == null)
            {
                await session.SendStringAsync(RespEncoder.EncodeArray(new object[] { "unsubscribe", channel, 0 }));
                return;
            }

            int subscriberCount = existingChannel.Unsubscribe(session);
            await session.SendStringAsync(RespEncoder.EncodeArray(new object[] { "unsubscribe", channel, subscriberCount }));
        }
    }
}
