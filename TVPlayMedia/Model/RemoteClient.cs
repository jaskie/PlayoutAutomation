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
                _clientSocket.OnError += _clientSocket_OnError;
                _clientSocket.Connect();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e, "Error initializing MediaManager remote interface");
            }
        }

        private void _clientSocket_OnError(object sender, ErrorEventArgs e)
        {
            Debug.WriteLine(e.Exception);
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
            {
                _messages[message.MessageGuid] = message;
                _messageHandler.Set();
                Debug.WriteLine(message, "Received");
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
            Func<T> resultFunc = new Func<T>(() =>
           {
               WebSocketMessage response;
               T responseObject = default(T);
               Stopwatch timeout = Stopwatch.StartNew();
               do
               {
                   _messageHandler.WaitOne(query_timeout);
                   if (_messages.TryRemove(messageGuid, out response))
                   {
                       responseObject = (response.Response is Newtonsoft.Json.Linq.JContainer) ?  JsonConvert.DeserializeObject<T>(response.Response.ToString()) : (T)Convert.ChangeType(response.Response, typeof(T));
                       if (responseObject is ProxyBase)
                           (responseObject as ProxyBase).SetClient(this);
                       if (responseObject is System.Collections.IEnumerable)
                           foreach (var o in responseObject as System.Collections.IEnumerable)
                               if (o is ProxyBase)
                                   (o as ProxyBase).SetClient(this);
                       return responseObject;
                   }
               }
               while (timeout.ElapsedMilliseconds < query_timeout);
               throw new TimeoutException(string.Format("Didn't received response from server within {0} milliseconds.", query_timeout));
           });
            IAsyncResult funcAsyncResult = resultFunc.BeginInvoke(null, null);
            funcAsyncResult.AsyncWaitHandle.WaitOne();
            return resultFunc.EndInvoke(funcAsyncResult);
        }

        public T GetInitalObject<T>()
        {
            WebSocketMessage query = new WebSocketMessage() { MessageType = WebSocketMessage.WebSocketMessageType.RootQuery };
            _clientSocket.Send(JsonConvert.SerializeObject(query));
            return WaitForResponse<T>(query.MessageGuid);
        }

        public T Query<T>(ProxyBase dto, [CallerMemberName] string methodName = "", params object[] parameters)
        {
            WebSocketMessage query = new WebSocketMessage() { DtoGuid = dto.GuidDto, MessageType = WebSocketMessage.WebSocketMessageType.Query, MethodName = methodName, Parameters = parameters };
            Debug.WriteLine(query, "Query");
            _clientSocket.Send(JsonConvert.SerializeObject(query));
            return WaitForResponse<T>(query.MessageGuid);
        }

        public T Get<T>(ProxyBase dto, [CallerMemberName] string propertyName = "")
        {
            WebSocketMessage query = new WebSocketMessage() { DtoGuid = dto.GuidDto, MessageType = WebSocketMessage.WebSocketMessageType.Get, MethodName = propertyName};
            Debug.WriteLine(query, "Get");
            _clientSocket.Send(JsonConvert.SerializeObject(query));
            return WaitForResponse<T>(query.MessageGuid);
        }

        public void Invoke(ProxyBase dto, [CallerMemberName] string methodName = "", params object[] parameters)
        {
            WebSocketMessage query = new WebSocketMessage() { DtoGuid = dto.GuidDto, MessageType = WebSocketMessage.WebSocketMessageType.Invoke, MethodName = methodName, Parameters = parameters };
            Debug.WriteLine(query, "Invoke");
            _clientSocket.Send(JsonConvert.SerializeObject(query));
        }

        public void Set(ProxyBase dto, object value, [CallerMemberName] string propertyName = "")
        {
            WebSocketMessage query = new WebSocketMessage() { DtoGuid = dto.GuidDto, MessageType = WebSocketMessage.WebSocketMessageType.Invoke, MethodName = propertyName, Parameters = new object[] { value} };
            Debug.WriteLine(query, "Set");
            _clientSocket.Send(JsonConvert.SerializeObject(query));
        }


    }


}
