using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using websocket_server;

namespace SimpleDiscordApi
{
    public static class DiscordRestApi
    {
        private static readonly string LibraryName = "CustomImplementation";

        public static void ChannelCreateMessage(string token, string channelId, string message)
        {
            string reqBody = $"{{\"content\":\"{message}\"}}";
            byte[] bodyByte = Encoding.ASCII.GetBytes(reqBody);
            int bodyLength = bodyByte.Length;
            string[] request = new string[]
            {
                $"POST /api/v9/channels/{channelId}/messages HTTP/1.1",
                $"Host: discord.com",
                $"Content-Type: application/json",
                $"Content-Length: {bodyLength}",
                $"User-Agent: {LibraryName}",
                $"Accept-Encoding: gzip",
                $"Connection: close",
                $"Authorization: Bot {token}"
            };
            SimpleHttpClient.DoRequest("discord.com", request, 443, bodyByte);
        }
    }
}
