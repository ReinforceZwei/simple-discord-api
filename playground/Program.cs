using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimpleDiscordApi;

namespace playground
{
    class Program
    {
        static void Main(string[] args)
        {
            string token = "";
            string channelId = "";
            string botOwner = ""; // Username with Tag

            var d = new DiscordGatewayApi(token);
            d.MessageCreate += (s, e) =>
            {
                Console.WriteLine($">>> MESSAGE\nAuthor: {e.Message.Author.UsernameWithTag}\nContent: {e.Message.Content}");
                if (!e.Message.Author.IsBot)
                {
                    if (e.Message.Author.UsernameWithTag == botOwner)
                        e.Message.Reply("Hello bot owner: " + e.Message.Content);
                    else
                        e.Message.Reply("echo: " + e.Message.Content);
                }
            };
            d.Ready += (s, e) => { Console.WriteLine("Ready"); };
            d.GatewayDisconnect += (s, e) => { Console.WriteLine($"Disconnected with gateway ({e.Code})"); };
            d.Connect();
            d.Identify();
            while (true)
            {
                var cmdRaw = Console.ReadLine();
                var cmdargs = cmdRaw.Split(new char[] { ' ' }).ToList();
                var cmd = cmdargs[0];
                cmdargs.RemoveAt(0);
                switch (cmd)
                {
                    case "reco": d.Reconnect(); break;
                    case "update": d.UpdatePresence(cmdargs[0]); break;
                    case "send":
                        {
                            DiscordRestApi.ChannelCreateMessage(token, channelId, string.Join(" ", cmdargs));
                            break;
                        }
                    case "dc": d.Disconnect(); break;
                    case "showsession": Console.WriteLine(d.sessionId); break;
                }
            }
        }
    }
}