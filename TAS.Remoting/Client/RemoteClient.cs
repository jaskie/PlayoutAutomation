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

        public event EventHandler<WebSocketMessageEventArgs> EventNotification;
        public event EventHandler OnOpen;
        public event EventHandler<CloseEventArgs> OnClose;
        
        public RemoteClient(string host)
        {
            _address = host;
            _serializer = JsonSerializer.CreateDefault();
            _serializer.Context = new StreamingContext(StreamingContextStates.Remoting, this);
            _serializer.PreserveReferencesHandling = PreserveReferencesHandling.Objects;
            _serializer.ReferenceResolver = new ReferenceResolver();
            _serializer.TypeNameHandling = TypeNameHandling.None;
        }

        public SerializationBinder Binder { get { return _serializer.Binder; }  set { _serializer.Binder = value; } }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Initialize()
        {
            try
            {
                if (_clientSocket != null)
                {
                    _clientSocket.OnOpen -= _clientSocket_OnOpen;
                    _clientSocket.OnClose -= _clientSocket_OnClose;  
                    _clientSocket.OnMessage -= _clientSocket_OnMessage;
                    _clientSocket.OnError -= _clientSocket_OnError;
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
            OnClose?.Invoke(this, e);
        }

        private string Serialize(object o)
        {
            using (System.IO.StringWriter writer = new System.IO.StringWriter())
            {
                _serializer.Serialize(writer, o);
                return writer.ToString();
            }
        }

        private void _clientSocket_OnMessage(object sender, MessageEventArgs e)
        {
            Debug.WriteLine(e.Data);
            WebSocketMessage message;
            using (StringReader stringReader = new StringReader(e.Data))
            using (JsonTextReader jsonReader = new JsonTextReader(stringReader))
            {
                message = _serializer.Deserialize<WebSocketMessage>(jsonReader);
            }
            if (message.MessageType == WebSocketMessage.WebSocketMessageType.EventNotification)
            {
                EventNotification?.Invoke(this, new WebSocketMessageEventArgs(message));
            }
            else
            {
                _receivedMessages[message.MessageGuid] = message;
                _messageHandler.Set();
            }
        }

        private void _clientSocket_OnOpen(object sender, EventArgs e)
        {
            OnOpen?.Invoke(this, EventArgs.Empty);
        }

        private WebSocketMessage WaitForResponse(WebSocketMessage sendedMessage)
        {
            // Func<WebSocketMessage> resultFunc = new Func<WebSocketMessage>(() =>
            //{
            WebSocketMessage response;
            Stopwatch timeout = Stopwatch.StartNew();
            if (_receivedMessages.TryRemove(sendedMessage.MessageGuid, out response))
                return response;
            do
            {
                _messageHandler.WaitOne(query_timeout);
                if (_receivedMessages.TryRemove(sendedMessage.MessageGuid, out response))
                    return response;
            }
            while (timeout.ElapsedMilliseconds < query_timeout);
            throw new TimeoutException($"Didn't received response from server within {query_timeout} milliseconds. Query was {sendedMessage}");
            //});
            // IAsyncResult funcAsyncResult = resultFunc.BeginInvoke(null, null);
            // funcAsyncResult.AsyncWaitHandle.WaitOne();
            // return resultFunc.EndInvoke(funcAsyncResult);
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
            _clientSocket.Send(Serialize(query));
            return MethodParametersAlignment.AlignType<T>(WaitForResponse(query).Response);
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
            _clientSocket.Send(Serialize(query));
            return MethodParametersAlignment.AlignType<T>(WaitForResponse(query).Response);
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
            _clientSocket.Send(Serialize(query));
            return MethodParametersAlignment.AlignType<T>(WaitForResponse(query).Response);
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
            _clientSocket.Send(Serialize(query));
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
            _clientSocket.Send(Serialize(query));
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
            _clientSocket.Send(Serialize(query));
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
            _clientSocket.Send(Serialize(query));
        }
    }


}
