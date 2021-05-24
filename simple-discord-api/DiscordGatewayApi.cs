using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using websocket_server;

namespace SimpleDiscordApi
{
    public class DiscordGatewayApi
    {
        private readonly string GatewayUrl = "wss://gateway.discord.gg/?v=9&encoding=json";
        private readonly string LibraryName = "CustomImplementation";
        private readonly string Token;

        private WebsocketConnection connection;
        private int lastSeq = -1;

        public DiscordGatewayApi(string token) 
        {
            Token = token;
        }

        public void Connect() 
        {
            connection = new WebsocketConnection(new Uri(GatewayUrl));
            connection.Connect();
            connection.Handshake();
            var hello = connection.ReadNextFrame(); // TODO: Handle heartbeat
            var json = Json.ParseJson(hello.DataAsString);
            int heartbeatInterval = int.Parse(json.Properties["d"].Properties["heartbeat_interval"].StringValue);
            new Thread(new ThreadStart(() =>
            {
                var rng = new Random();
                Thread.Sleep((int)Math.Floor(rng.NextDouble() * heartbeatInterval));
                while (true)
                {
                    string seq = lastSeq == -1 ? "null" : lastSeq.ToString();
                    Console.WriteLine("<<<<<< Heartbeat SEQ: " + seq);
                    connection.Send("{\"op\":1,\"d\":" + seq + "}");
                    Thread.Sleep(heartbeatInterval);
                }
            })).Start();
        }

        public void Identify()
        {
            Dictionary<string, object> req = new Dictionary<string, object>()
            {
                { "op", (int)GatewayOpcode.Identify },
                { 
                    "d", new Dictionary<string, object>()
                    {
                        { "token", Token },
                        { "intents", (1 << 9) | (1 << 12) }, // Guild Message and Direct Message
                        {
                            "properties", new Dictionary<string, object>()
                            {
                                { "$os", "linux" },
                                { "$browser", LibraryName },
                                { "$device", LibraryName }
                            }
                        }
                    } 
                }
            };
            string json = JsonMaker.ToJson(req);
            connection.Send(json);
            var resp = connection.ReadNextFrame();
            var j = Json.ParseJson(resp.DataAsString);
            Console.WriteLine(">>>>>>");
            Console.WriteLine(resp.DataAsString);
            //try
            //{
            // TODO: Error handling (e.g. Invalid token)
            // TODO: Save session ID somewhere
            string sessionId = j.Properties["d"].Properties["session_id"].StringValue; 
            Ready?.Invoke(this, new ReadyEventArgs(sessionId));
            //}
            //catch { }
            connection.Message += _OnWsMessage;
            connection.StartReceiveAsync();
            UpdatePresence();
        }

        public void UpdatePresence(string status = "online")
        {
            Dictionary<string, object> req = new Dictionary<string, object>()
            {
                { "op", (int)GatewayOpcode.PresenceUpdate },
                {
                    "d", new Dictionary<string, object>()
                    {
                        { "since", null },
                        { "activities", new List<object>() }, 
                        { "status", status },
                        { "afk", false }
                    }
                }
            };
            string json = JsonMaker.ToJson(req);
            connection.Send(json);
        }

        private void _OnWsMessage(object s, TextMessageEventArgs e)
        {
            Console.WriteLine(">>>>>> WS Message");
            Console.WriteLine(e.Message);
            var json = Json.ParseJson(e.Message);
            if (json.Properties["op"].StringValue == ((int)GatewayOpcode.Dispatch).ToString()) // Dispatch event
            {
                lastSeq = int.Parse(json.Properties["s"].StringValue);
                string eventName = json.Properties["t"].StringValue;
                if (eventName == "MESSAGE_CREATE")
                {
                    Console.WriteLine(">>> Incomming Message");
                    var msgObj = json.Properties["d"];
                    string channelId = msgObj.Properties["channel_id"].StringValue;
                    string content = msgObj.Properties["content"].StringValue;
                    string sender = msgObj.Properties["author"].Properties["username"].StringValue;
                    string senderId = msgObj.Properties["author"].Properties["id"].StringValue;
                    string senderTag = msgObj.Properties["author"].Properties["discriminator"].StringValue;
                    var eventArgs = new MessageCreateEventArgs(channelId, content, sender, senderId, senderTag);
                    MessageCreate?.Invoke(this, eventArgs);
                    Console.WriteLine($"Channel ID: {channelId}\nContent: {content}\nAuthor: {sender}");
                }
            }
        }

        private void _OnWsDisconnect(object s, DisconnectEventArgs e)
        {
            Console.WriteLine("Disconnected with Gateway");
        }

        public event EventHandler<ReadyEventArgs> Ready;
        public event EventHandler<MessageCreateEventArgs> MessageCreate;
    }

    public class ReadyEventArgs : EventArgs
    {
        public readonly string SessionId;
        public ReadyEventArgs(string sessionId)
        {
            SessionId = sessionId;
        }
    }

    public class MessageCreateEventArgs : EventArgs
    {
        public readonly string ChannelId;
        public readonly string Content;
        public readonly string Author;
        public readonly string AuthorId;
        public readonly string AuthorTag;
        public MessageCreateEventArgs() { }
        public MessageCreateEventArgs(string channelId, string content, string author, string authorId, string authorTag)
        {
            ChannelId = channelId;
            Content = content;
            Author = author;
            AuthorId = authorId;
            AuthorTag = authorTag;
        }
    }
}
