using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using websocket_server;

namespace SimpleDiscordApi
{
    public class User
    {
        public readonly ulong Id;
        public readonly string Username;
        public readonly string Tag;
        public readonly bool IsBot = false;

        public string UsernameWithTag
        {
            get
            {
                return string.Concat(Username, "#", Tag);
            }
        }

        public User(string id, string username, string tag)
        {
            Id = ulong.Parse(id);
            Username = username;
            Tag = tag;
        }

        public User(string id, string username, string tag, bool bot)
            : this(id, username, tag)
        {
            IsBot = bot;
        }

        public static User ParseUser(Json.JSONData userObject)
        {
            string sender = userObject.Properties["username"].StringValue;
            string senderId = userObject.Properties["id"].StringValue;
            string senderTag = userObject.Properties["discriminator"].StringValue;
            bool senderIsBot = false;
            if (userObject.Properties.ContainsKey("bot"))
            {
                if (userObject.Properties["bot"].StringValue == "true")
                    senderIsBot = true;
            }
            return new User(senderId, sender, senderTag, senderIsBot);
        }
    }
}
