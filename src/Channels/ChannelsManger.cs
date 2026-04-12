using codecrafters_redis.src.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace codecrafters_redis.src.Channels
{
    public static class ChannelsManger
    {
        private static readonly Dictionary<string, Channel> channels = new Dictionary<string, Channel>();

        public static Channel GetOrCreateChannel(string name)
        {
            lock (channels)
            {
                if (!channels.TryGetValue(name, out var channel))
                {
                    channel = new Channel();
                    channels[name] = channel;
                }
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
        public void Publish(string message)
        {
            var subscribersCopy = GetSubscribers();
            foreach (var subscriber in subscribersCopy)
            {
                subscriber.SendStringAsync(message);
            }
        }
        public int Subscribe(ClientSession session)
        {
            lock (subscribers)
            {
                subscribers.Add(session);
                session.ChannelSubscriptionCount++;
                return session.ChannelSubscriptionCount;
            }
        }
        public void Unsubscribe(ClientSession session)
        {
            lock (subscribers)
            {
                subscribers.Remove(session);
                session.ChannelSubscriptionCount--;
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
