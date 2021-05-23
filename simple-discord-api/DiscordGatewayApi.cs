using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using websocket_server;

namespace SimpleDiscordApi
{
    public class DiscordGatewayApi
    {
        private readonly string GatewayUrl = "wss://gateway.discord.gg/?v=9&encoding=json";
        private readonly string LibraryName = "CustomImplementation";

        private WebsocketConnection connection;

        public DiscordGatewayApi() { }

        public void Connect() 
        {
            connection = new WebsocketConnection(new Uri(GatewayUrl));
            connection.Connect();
            connection.Handshake();
            var hello = connection.ReadNextFrame();
        }

        public void Identify()
        {
            Dictionary<string, object> req = new Dictionary<string, object>()
            {
                { "op", 2 }, // TODO: Use Enum for op
                { 
                    "d", new Dictionary<string, object>()
                    {
                        { "token", "" },
                        { "intents", 513 }, // TODO: Calculate correct intents for message create event only
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
            Console.WriteLine(json);
        }
    }
}
