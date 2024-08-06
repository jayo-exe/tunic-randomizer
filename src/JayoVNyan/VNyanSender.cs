using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using WebSocketSharp;

namespace JayoVNyan {

    
    public static class VNyanSender
    {
        public static event Action<string> LogInfo;
        public static event Action<string> OnWebsocketMessage;
        public static event Action OnWebsocketOpen;
        public static event Action<string> OnWebsocketClose;
        public static event Action<string> OnWebsocketError;

        private static WebSocket socket;
        
        public static void connectSocket(string url)
        {
            if (socket == null || socket.ReadyState != WebSocketState.Open)
            {
                try
                {
                    DisconnectSocket();
                    
                    socket = new WebSocket(url);
                    
                    socket.OnMessage += ReceiveMessage;
                    socket.OnOpen += SocketOpened;
                    socket.OnClose += SocketClosed;
                    socket.OnError += SocketError;
                    socket.Connect();

                }
                catch (Exception e)
                {
                    LogInfo.Invoke($"could not connect to VNyan; {e.Message}");
                }
            }
        }

        public static void DisconnectSocket()
        {
            if (socket == null || socket.ReadyState != WebSocketState.Open)
            {
                try
                {
                    if (socket != null)
                    {
                        socket.OnMessage -= ReceiveMessage;
                        socket.OnOpen -= SocketOpened;
                        socket.OnClose -= SocketClosed;
                        socket.OnError -= SocketError;
                        socket.Close();
                    }
                }
                catch (Exception e)
                {
                    LogInfo.Invoke($"could not disconnect from VNyan; {e.Message}");
                }
            }
        }


        public static void SendActionToVNyan(string action, object payload)
        {
            Dictionary<string, string> payloadDictionary = payload.GetType().GetProperties()
            .ToDictionary(x => x.Name, x => x.GetValue(payload)?.ToString() ?? "");

            payloadDictionary.Add("action", action);
            string messageJson = JsonSerializer.Serialize(payloadDictionary);

            try
            {
                socket.SendAsync(messageJson, null);
            } 
            catch (Exception e)
            {
                LogInfo.Invoke($"could not send to VNyan; {e.Message}");
            }
            
        }

        public static void SendActionToVNyan(string action, Dictionary<string, string> payloadDictionary)
        {
            payloadDictionary.Add("action", action);
            string messageJson = JsonSerializer.Serialize(payloadDictionary);

            try
            {
                socket.SendAsync(messageJson, null);
            }
            catch (Exception e)
            {
                LogInfo.Invoke($"could not send to VNyan; {e.Message}");
            }

        }

        public static void SocketOpened(object sender, EventArgs e)
        {
            OnWebsocketOpen.Invoke();
        }
        public static void SocketClosed(object sender, CloseEventArgs e)
        {
            OnWebsocketClose.Invoke(e.Reason);
        }
        public static void SocketError(object sender, ErrorEventArgs e)
        {
            OnWebsocketError.Invoke(e.Message);
        }
        public static void ReceiveMessage(object sender, MessageEventArgs e) {
            OnWebsocketMessage.Invoke(e.Data);
        }


    }
}