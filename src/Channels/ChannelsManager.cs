using codecrafters_redis.src.Client;
using codecrafters_redis.src.Resp;
using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src.Channels
{
    public static class ChannelsManager
    {
        private static readonly Dictionary<string, Channel> channels = new Dictionary<string, Channel>();

        public static Channel GetOrCreateChannel(string name)
        {
            lock (channels)
            {
                if (!channels.TryGetValue(name, out var channel))
                {
                    channel = new Channel(name);
                    channels[name] = channel;
                }
                return channel;
            }
        }

        public static Channel? GetChannel(string name)
        {
            lock (channels)
            {
                channels.TryGetValue(name, out var channel);
                return channel;
            }
        }

        public static void RemoveChannel(string name)
        {
            lock (channels)
            {
                channels.Remove(name);
            }
        }
        
    }

    public class Channel
    {
        private readonly HashSet<ClientSession> subscribers = new HashSet<ClientSession>();

        public string Name { get; set; }
        public int SubscriberCount
        {
            get
            {
                lock (subscribers)
                {
                    return subscribers.Count;
                }
            }
        }

        public Channel(string name)
        {
            Name = name;
        }
        public int Publish(string message)
        {
            var subscribersCopy = GetSubscribers();
            foreach (var subscriber in subscribersCopy)
            {
                subscriber.SendStringAsync(RespEncoder.EncodeArray(new object[] { "message", Name, message }));
            }
            return SubscriberCount;
        
            
        }
        public int Subscribe(ClientSession session)
        {
            lock (subscribers)
            {
                if (subscribers.Add(session)) session.ChannelSubscriptionCount++;

                return session.ChannelSubscriptionCount;
            }
        }
        public int Unsubscribe(ClientSession session)
        {
            lock (subscribers)
            {
                if (subscribers.Remove(session)) session.ChannelSubscriptionCount--;

                if (subscribers.Count == 0)
                {
                    ChannelsManager.RemoveChannel(Name);
                }
                return session.ChannelSubscriptionCount;
            }
        }
 
        public IEnumerable<ClientSession> GetSubscribers()
        {
            lock (subscribers)
            {
                return subscribers.ToArray();
            }
        }
    }
}
