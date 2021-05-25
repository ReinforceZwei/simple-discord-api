using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using websocket_server;

namespace SimpleDiscordApi
{
    public class Message
    {
        private DiscordGatewayApi Gateway;
        public readonly ulong Id;
        public readonly ulong ChannelId;
        public readonly User Author;
        public readonly string Content;
        public readonly string Timestamp;

        public Message(DiscordGatewayApi gateway, string id, string channelId, User author, string content, string timestamp)
        {
            Gateway = gateway;
            Id = ulong.Parse(id);
            ChannelId = ulong.Parse(channelId);
            Author = author;
            Content = content;
            Timestamp = timestamp;
        }

        public void Reply(string content)
        {
            DiscordRestApi.ChannelCreateMessage(Gateway.Token, ChannelId.ToString(), content);
        }

        public static Message ParseMessage(DiscordGatewayApi gateway, Json.JSONData messageObject)
        {
            string id = messageObject.Properties["id"].StringValue;
            string channelId = messageObject.Properties["channel_id"].StringValue;
            string content = messageObject.Properties["content"].StringValue;
            string timestamp = messageObject.Properties["timestamp"].StringValue;
            User user = User.ParseUser(messageObject.Properties["author"]);
            return new Message(gateway, id, channelId, user, content, timestamp);
        }
    }
}
