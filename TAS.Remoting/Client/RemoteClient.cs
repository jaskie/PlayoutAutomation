#undef DEBUG

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using WebSocketSharp;
using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace TAS.Remoting.Client
{
    public class RemoteClient
    {
        readonly string _address;
        WebSocket _clientSocket;
        AutoResetEvent _messageHandler = new AutoResetEvent(false);
        readonly JsonSerializer _serializer;
        ConcurrentDictionary<Guid, WebSocketMessage> _receivedMessages = new ConcurrentDictionary<Guid, WebSocketMessage>();
        const int query_timeout = 150000;
        readonly ConcurrentDictionary<Guid, ProxyBase> _knownDtos = new ConcurrentDictionary<Guid, ProxyBase>();

        public event EventHandler<WebSocketMessageEventArgs> EventNotification;
        public event EventHandler OnOpen;
        public event EventHandler<CloseEventArgs> OnClose;
        
        public RemoteClient(string host)
        {
            _address = host;
            _serializer = JsonSerializer.CreateDefault();
            //_serializer.Converters.Add(new ClientSerializationConverter());
        }

        public SerializationBinder Binder { get { return _serializer.Binder; }  set { _serializer.Binder = value; } }

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
                _clientSocket = new WebSocket(string.Format("ws://{0}/Engine", _address));
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

        private void _clientSocket_OnError(object sender, WebSocketSharp.ErrorEventArgs e)
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
            Debug.WriteLine(e.Data);
            WebSocketMessage message;
            using (StringReader stringReader = new StringReader(e.Data))
            using (JsonTextReader jsonReader = new JsonTextReader(stringReader))
            {
                message = _serializer.Deserialize<WebSocketMessage>(jsonReader);
                _registerResponse(ref message.Response);
            }
            if (message.MessageType == WebSocketMessage.WebSocketMessageType.EventNotification)
            {
                var h = EventNotification;
                if (h != null)
                    h(this, new WebSocketMessageEventArgs(message));
            }
            else
            {
                _receivedMessages[message.MessageGuid] = message;
                _messageHandler.Set();
            }
        }

        private void _clientSocket_OnOpen(object sender, EventArgs e)
        {
            var h = OnOpen;
            if (h != null)
                h(this, EventArgs.Empty);
        }

        private WebSocketMessage WaitForResponse(WebSocketMessage sendedMessage)
        {
            Func<WebSocketMessage> resultFunc = new Func<WebSocketMessage>(() =>
           {
               WebSocketMessage response;
               Stopwatch timeout = Stopwatch.StartNew();
               do
               {
                   _messageHandler.WaitOne(query_timeout);
                   if (_receivedMessages.TryRemove(sendedMessage.MessageGuid, out response))
                       return response;
               }
               while (timeout.ElapsedMilliseconds < query_timeout);
               throw new TimeoutException(string.Format("Didn't received response from server within {0} milliseconds. Query was {1}", query_timeout, sendedMessage));
           });
            IAsyncResult funcAsyncResult = resultFunc.BeginInvoke(null, null);
            funcAsyncResult.AsyncWaitHandle.WaitOne();
            return resultFunc.EndInvoke(funcAsyncResult);
        }

        public void Update(object serialized, object target)
        {
            if (serialized is Newtonsoft.Json.Linq.JContainer)
                using (StringReader stringReader = new StringReader(serialized.ToString()))
                    _serializer.Populate(stringReader, target);
        }

        public T GetInitalObject<T>()
        {
            WebSocketMessage query = new WebSocketMessage() { MessageType = WebSocketMessage.WebSocketMessageType.RootQuery };
            _clientSocket.Send(JsonConvert.SerializeObject(query));
            return (T)WaitForResponse(query).Response;
        }

        public T Query<T>(ProxyBase dto, string methodName, params object[] parameters)
        {
            WebSocketMessage query = new WebSocketMessage()
            {
                DtoGuid = dto.DtoGuid,
                MessageType = WebSocketMessage.WebSocketMessageType.Query,
                MemberName = methodName,
                Parameters = parameters,
#if DEBUG
                DtoName = dto.ToString()
#endif
            };
            _clientSocket.Send(JsonConvert.SerializeObject(query));
            return (T)WaitForResponse(query).Response;
        }

        public T Get<T>(ProxyBase dto, string propertyName)
        {
            WebSocketMessage query = new WebSocketMessage()
            {
                DtoGuid = dto.DtoGuid,
                MessageType = WebSocketMessage.WebSocketMessageType.Get,
                MemberName = propertyName,
#if DEBUG
                DtoName = dto.ToString()
#endif
            };
            _clientSocket.Send(JsonConvert.SerializeObject(query));
            object response = WaitForResponse(query).Response;
            if (response is long)
                return (T)Convert.ChangeType(response, typeof(T));
            
            return (T)response;
        }

        public void Invoke(ProxyBase dto, string methodName, params object[] parameters)
        {
            WebSocketMessage query = new WebSocketMessage()
            {
                DtoGuid = dto.DtoGuid,
                MessageType = WebSocketMessage.WebSocketMessageType.Invoke,
                MemberName = methodName,
                Parameters = parameters,
#if DEBUG
                DtoName = dto.ToString()
#endif
            };
            _clientSocket.Send(JsonConvert.SerializeObject(query));
        }

        public void Set(ProxyBase dto, object value, string propertyName)
        {
            WebSocketMessage query = new WebSocketMessage()
            {
                DtoGuid = dto.DtoGuid,
                MessageType = WebSocketMessage.WebSocketMessageType.Set,
                MemberName = propertyName,
                Parameters = new object[] { value },
#if DEBUG
                DtoName = dto.ToString()
#endif
            };
            _clientSocket.Send(JsonConvert.SerializeObject(query));
        }

        public void EventAdd(ProxyBase dto, string eventName)
        {
            WebSocketMessage query = new WebSocketMessage()
            {
                DtoGuid = dto.DtoGuid,
                MessageType = WebSocketMessage.WebSocketMessageType.EventAdd,
                MemberName = eventName,
#if DEBUG
                DtoName = dto.ToString()
#endif
            };
            _clientSocket.Send(JsonConvert.SerializeObject(query));
            if (eventName == "PropertyChanged")
                Update(WaitForResponse(query).Response, dto);
        }

        public void EventRemove(ProxyBase dto, string eventName)
        {
            WebSocketMessage query = new WebSocketMessage()
            {
                DtoGuid = dto.DtoGuid,
                MessageType = WebSocketMessage.WebSocketMessageType.EventRemove,
                MemberName = eventName,
#if DEBUG
                DtoName = dto.ToString()
#endif
            };
            _clientSocket.Send(JsonConvert.SerializeObject(query));
        }

        public bool TryGetObject(Guid guid, out ProxyBase dto)
        {
            if (_knownDtos.TryGetValue(guid, out dto))
                return true;
            else
            {
                dto = null;
                return false;
            }
        }

        public void SetObject(ProxyBase proxy)
        {
            _knownDtos[proxy.DtoGuid] = proxy;
            proxy.SetClient(this);
        }

        private void _registerResponse(ref object response)
        {
            ProxyBase proxy = response as ProxyBase;
            if (proxy != null)
            {
                ProxyBase oldObject;
                if (TryGetObject(proxy.DtoGuid, out oldObject))
                    response = oldObject;
                else
                    SetObject(proxy);
            }
            System.Collections.IList list = response as System.Collections.IList;
            if (list != null)
                for (int i = 0; i < list.Count; i++)
                {
                    object listElement = list[i];
                    _registerResponse(ref listElement);
                    list[i] = listElement;
                }
        }


    }


}
