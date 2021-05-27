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
        internal readonly string LibraryName = "CustomImplementation";
        internal readonly string Token;

        public string sessionId;

        private WebsocketConnection connection;
        private int lastSeq = -1;
        private bool lastHeartBeatAck = true;

        private Thread HeartbeatThread;

        private static bool debug = true;

        public DiscordGatewayApi(string token) 
        {
            Token = token;
        }

        public void Connect() 
        {
            connection = new WebsocketConnection(new Uri(GatewayUrl));
            connection.Connect();
            connection.Handshake();
            connection.DisconnectEvent += _OnWsDisconnect;

            var hello = connection.ReadNextFrame();
            var json = Json.ParseJson(hello.DataAsString);
            int heartbeatInterval = int.Parse(json.Properties["d"].Properties["heartbeat_interval"].StringValue);
            StartHeartbeat(heartbeatInterval);
        }

        public bool Identify()
        {
            var data = new Dictionary<string, object>()
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
            };

            string json = GetPayloadString(GatewayOpcode.Identify, data);
            connection.Send(json);
            var resp = connection.ReadNextFrame();
            if (resp.Opcode == Opcode.Text)
            {
                var j = Json.ParseJson(resp.DataAsString);
                DebugLog(">>>>>> READY");
                DebugLog(resp.DataAsString);
                // TODO: Save session ID somewhere
                sessionId = j.Properties["d"].Properties["session_id"].StringValue;
                lastSeq = -1;

                Ready?.Invoke(this, new ReadyEventArgs(sessionId));
                connection.TextMessage += _OnWsMessage;
                connection.StartReceiveAsync();
                UpdatePresence();
                return true;
            }
            else if (resp.Opcode == Opcode.ClosedConnection)
            {
                int code = resp.CloseStatusCode ?? -1;
                DebugLog("Identity failed with code " + code);
                // Still in custom message handling
                connection.Send(Opcode.ClosedConnection);
                connection.Close();
                return false;
            }
            else
            {
                // WS opcode other than text and close
                connection.Disconnect();
                return false;
            }
        }

        public void UpdatePresence(string status = "online")
        {
            var data = new Dictionary<string, object>()
            {
                { "since", null },
                { "activities", new List<object>() },
                { "status", status },
                { "afk", false }
            };
            string json = GetPayloadString(GatewayOpcode.PresenceUpdate, data);
            //Console.WriteLine("<<<<<< Update presence");
            //Console.WriteLine(json);
            connection.Send(json);
        }

        public void Resume()
        {
            // Assume connected to gateway, but not yet start receive
            if (!string.IsNullOrEmpty(sessionId))
            {
                var data = new Dictionary<string, object>()
                {
                    { "token", Token },
                    { "session_id", sessionId },
                    { "seq", lastSeq }
                };
                string json = GetPayloadString(GatewayOpcode.Resume, data);
                connection.Send(json);
                var nextResp = connection.ReadNextFrame();
                DebugLog(">>>>>> RESUME RESULT");
                DebugLog(nextResp.DataAsString);
                var j = Json.ParseJson(nextResp.DataAsString);
                if (j.Properties["op"].StringValue == ((int)GatewayOpcode.InvalidSession).ToString())
                {
                    DebugLog("Resume result: Invalid session");
                    Thread.Sleep(2500);
                    Identify();
                }
                else
                {
                    DebugLog("Gateway Resumed");
                    _OnWsMessage(this, new TextMessageEventArgs() { Client = connection, Message = nextResp.DataAsString });
                    connection.TextMessage += _OnWsMessage;
                    connection.StartReceiveAsync();
                    UpdatePresence();
                }
            }
            else
            {
                Identify();
            }
        }

        public void Reconnect()
        {
            // reconnect to gateway
            connection.Disconnect(1000);
            connection = null;

            try
            {
                HeartbeatThread.Abort();
            }
            catch { }
            HeartbeatThread = null;
            lastHeartBeatAck = true;
            Connect();
            Resume();
        }

        public void Disconnect()
        {
            connection.Disconnect(1000);
        }

        private void StartHeartbeat(int interval)
        {
            HeartbeatThread = new Thread(new ThreadStart(() =>
            {
                var rng = new Random();
                Thread.Sleep((int)Math.Floor(rng.NextDouble() * interval));
                while (true)
                {
                    if (connection.State != State.Open)
                    {
                        DebugLog("WS state not open, heartbeat thread abort");
                        HeartbeatThread.Abort();
                        break;
                    }
                    if (!lastHeartBeatAck)
                    {
                        DebugLog("Last heartbeat ACK not received, reconnect");
                        Reconnect();
                        break;
                    }
                    string seq = lastSeq == -1 ? "null" : lastSeq.ToString();
                    DebugLog("<<<<<< Heartbeat SEQ: " + seq);
                    connection.Send("{\"op\":1,\"d\":" + seq + "}");
                    lastHeartBeatAck = false;
                    Thread.Sleep(interval);
                }
            }));
            HeartbeatThread.Start();
        }

        private void _OnWsMessage(object s, TextMessageEventArgs e)
        {
            //Console.WriteLine(">>>>>> WS Message");
            //Console.WriteLine(e.Message);
            if (string.IsNullOrEmpty(e.Message))
            {
                DebugLog("Empty message in WsMessage!");
                return;
            }
            var json = Json.ParseJson(e.Message);
            GatewayOpcode opcode = (GatewayOpcode)int.Parse(json.Properties["op"].StringValue);

            switch (opcode)
            {
                case GatewayOpcode.Dispatch:
                    {
                        lastSeq = int.Parse(json.Properties["s"].StringValue);
                        string eventName = json.Properties["t"].StringValue;
                        if (eventName == "MESSAGE_CREATE")
                        {
                            var msgObj = json.Properties["d"];
                            Message message = Message.ParseMessage(this, msgObj);
                            var eventArgs = new MessageCreateEventArgs(message);
                            MessageCreate?.Invoke(this, eventArgs);
                            //Console.WriteLine(">>> Incomming Message");
                            //Console.WriteLine($"Channel ID: {message.ChannelId}\nContent: {message.Content}\nAuthor: {message.Author.Username}");
                        }
                        break;
                    }
                case GatewayOpcode.HeartbeatACK:
                    {
                        lastHeartBeatAck = true;
                        break;
                    }
                case GatewayOpcode.Reconnect:
                    {
                        // Should be a rare event
                        DebugLog("Gateway asked for reconnect");
                        Reconnect();
                        break;
                    }
            }
        }

        private void _OnWsDisconnect(object s, DisconnectEventArgs e)
        {
            // Disconnected after few momment after reconnect
            DebugLog("Disconnected with Gateway: " + e.Code);
            GatewayDisconnect?.Invoke(this, new GatewatDisconnectEventArgs(e.Code));
        }

        private static string GetPayloadString(GatewayOpcode opcode, object data)
        {
            var payload = new Dictionary<string, object>()
            {
                { "op", (int)opcode },
                { "d", data }
            };
            return JsonMaker.ToJson(payload);
        }

        private static void DebugLog<T>(T text)
        {
            if (debug)
                Console.WriteLine(text);
        }

        public static void SetDebugMode(bool enable)
        {
            debug = enable;
        }

        /// <summary>
        /// Emitted when the gateway is ready to send and receive event
        /// </summary>
        public event EventHandler<ReadyEventArgs> Ready;

        /// <summary>
        /// Emitted when a new message was created
        /// </summary>
        public event EventHandler<MessageCreateEventArgs> MessageCreate;

        /// <summary>
        /// Emitted when disconnected with gateway. You do not need to reconnect yourself as it is handled automatically
        /// </summary>
        public event EventHandler<GatewatDisconnectEventArgs> GatewayDisconnect;
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
        public Message Message;
        public MessageCreateEventArgs() { }
        public MessageCreateEventArgs(Message message)
        {
            Message = message;
        }
    }

    public class GatewatDisconnectEventArgs : EventArgs 
    {
        public int Code;
        public GatewatDisconnectEventArgs(int code)
        {
            Code = code;
        }
    }
}
