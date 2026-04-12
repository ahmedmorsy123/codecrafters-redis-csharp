using codecrafters_redis.src.Channels;
using codecrafters_redis.src.Client;
using codecrafters_redis.src.Resp;
using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src.Commands
{
    public class SUBSCRIBE : ICommand
    {
        public string Name => "SUBSCRIBE";

        public bool IsWrite => false;

        public async Task ExecuteAsync(string[] args, ClientSession session)
        {
            if (args.Length != 1)
            {
                await session.SendStringAsync("-ERR wrong number of arguments for 'SUBSCRIBE' command\r\n");
                return;
            }

            string channel = args[0];
            int channelSubscriptionCount = ChannelsManger.GetOrCreateChannel(channel).Subscribe(session);
            await session.SendStringAsync(RespEncoder.EncodeArray(new object[] { "subscribe", channel, channelSubscriptionCount }));
        }
    }
}
