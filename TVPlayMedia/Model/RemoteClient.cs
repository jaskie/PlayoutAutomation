using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using TAS.Common;
using TAS.Server.Common;
using TAS.Server.Interfaces;
using TAS.Server.Remoting;
using WebSocketSharp;

namespace TAS.Client.Model
{
    public class RemoteClient: IRemoteClient
    {
        readonly string _address;
        WebSocket _clientSocket;
        AutoResetEvent _messageHandler = new AutoResetEvent(false);
        ConcurrentDictionary<Guid, WebSocketMessage> _messages = new ConcurrentDictionary<Guid, WebSocketMessage>();
        const int query_timeout = 5000;

        public event EventHandler<WebSocketMessageEventArgs> OnMessage;
        public event EventHandler OnOpen;
        public event EventHandler<CloseEventArgs> OnClose;
        
        public RemoteClient(string host)
        {
            _address = host;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public void Initialize()
        {
            try
            {
                if (_clientSocket != null)
                {
                    _clientSocket.OnOpen -= _clientSocket_OnOpen;
                    _clientSocket.OnClose -= _clientSocket_OnClose;  
                    _clientSocket.OnMessage -= _clientSocket_OnMessage;
                }
                _clientSocket = new WebSocket(string.Format("ws://{0}/MediaManager", _address));
                _clientSocket.OnOpen += _clientSocket_OnOpen;
                _clientSocket.OnClose += _clientSocket_OnClose;
                _clientSocket.OnMessage += _clientSocket_OnMessage;
                _clientSocket.Connect();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e, "Error initializing MediaManager remote interface");
            }
        }

        private void _clientSocket_OnClose(object sender, CloseEventArgs e)
        {
            var h = OnClose;
            if (h != null)
                h(this, e);
        }

        private void _clientSocket_OnMessage(object sender, MessageEventArgs e)
        {
            WebSocketMessage message =  JsonConvert.DeserializeObject<WebSocketMessage>(e.Data);
            if (message.MessageType== WebSocketMessage.WebSocketMessageType.Notification)
            {
                var h = OnMessage;
                if (h != null)
                    h(this, new WebSocketMessageEventArgs(message));
            }
            else
            if (message.MessageType == WebSocketMessage.WebSocketMessageType.Response)
            {
                _messages[message.MessageGuid] = message;
                _messageHandler.Set();
            }
            else
            if (message.MessageType == WebSocketMessage.WebSocketMessageType.InitalTransfer)
            {
                _messages[Guid.Empty] = message;
                _messageHandler.Set();
            }
        }

        private void _clientSocket_OnOpen(object sender, EventArgs e)
        {
            var h = OnOpen;
            if (h != null)
                h(this, EventArgs.Empty);
        }

        private T WaitForResponse<T>(Guid messageGuid)
        {
            WebSocketMessage response;
            T responseObject = default(T);
            do
            {
                _messageHandler.WaitOne(query_timeout);
                if (_messages.TryRemove(messageGuid, out response))
                {
                    responseObject = JsonConvert.DeserializeObject<T>(response.Response.ToString());
                    if (responseObject is DtoBase)
                        (responseObject as DtoBase).SetClient(this);
                }
            }
            while (response == null);
            return responseObject;
        }

        public T GetInitalObject<T>()
        {
            return WaitForResponse<T>(Guid.Empty);
        }

        public T Query<T>(DtoBase dto, [CallerMemberName] string methodName = "", params object[] parameters)
        {
            WebSocketMessage query = new WebSocketMessage() { DtoGuid = dto.GuidDto, MessageType = WebSocketMessage.WebSocketMessageType.Query, MethodName = methodName, Parameters = parameters };
            Debug.WriteLine(query.MessageGuid, "Send Guid");
            _clientSocket.Send(JsonConvert.SerializeObject(query));
            return WaitForResponse<T>(query.MessageGuid);
        }

    }


}
