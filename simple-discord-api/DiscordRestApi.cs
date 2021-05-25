using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using websocket_server;

namespace SimpleDiscordApi
{
    public static class DiscordRestApi
    {
        private static readonly string ApiHost = "discord.com";
        private static readonly string ApiEndpoint = "/api/v9";
        private static readonly string LibraryName = "CustomImplementation";

        public static bool ChannelCreateMessage(string token, string channelId, string message)
        {
            string reqBody = $"{{\"content\":\"{message}\"}}";
            byte[] bodyByte = Encoding.ASCII.GetBytes(reqBody);
            int bodyLength = bodyByte.Length;
            string[] request = MakeRequestHeaders(token, "POST", $"/channels/{channelId}/messages", bodyLength);
            var response = SimpleHttpClient.DoRequest("discord.com", request, 443, bodyByte);
            return response.Status >= 200 && response.Status <= 299;
        }

        private static string[] MakeRequestHeaders(string token, string method, string endpoint, int payloadLength)
        {
            return new string[]
            {
                $"{method} {ApiEndpoint}{endpoint} HTTP/1.1",
                $"Host: {ApiHost}",
                $"Content-Type: application/json",
                $"Content-Length: {payloadLength}",
                $"User-Agent: {LibraryName}",
                $"Accept-Encoding: gzip",
                $"Connection: close",
                $"Authorization: Bot {token}"
            };
        }
    }
}
