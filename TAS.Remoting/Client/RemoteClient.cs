//#undef DEBUG
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
using Newtonsoft.Json.Serialization;

namespace TAS.Remoting.Client
{
    public class RemoteClient: IDisposable
    {
        readonly WebSocket _clientSocket;
        AutoResetEvent _messageHandler = new AutoResetEvent(false);
        readonly JsonSerializer _serializer;
        readonly ReferenceResolver _referenceResolver;
        ConcurrentDictionary<Guid, WebSocketMessage> _receivedMessages = new ConcurrentDictionary<Guid, WebSocketMessage>();

        const int query_timeout =
#if DEBUG 
            50000
#else
            3000
#endif
            ;

        internal event EventHandler<WebSocketMessageEventArgs> EventNotification;
        public event EventHandler Connected;
        public event EventHandler Disconnected;
        
        public RemoteClient(string host)
        {
            _serializer = JsonSerializer.CreateDefault();
            _serializer.Context = new StreamingContext(StreamingContextStates.Remoting, this);
            _serializer.PreserveReferencesHandling = PreserveReferencesHandling.Objects;
            _referenceResolver = new ReferenceResolver();
            _serializer.ReferenceResolver = _referenceResolver;
            _serializer.TypeNameHandling = TypeNameHandling.None;
#if DEBUG
            _serializer.Formatting = Formatting.Indented;
#endif
            _clientSocket = new WebSocket(string.Format("ws://{0}/Engine", host)) { Compression = CompressionMethod.Deflate, WaitTime = TimeSpan.FromMilliseconds(query_timeout) };
            _clientSocket.OnOpen += _clientSocket_OnOpen;
            _clientSocket.OnClose += _clientSocket_OnClose;
            _clientSocket.OnMessage += _clientSocket_OnMessage;
            _clientSocket.OnError += _clientSocket_OnError;
            Debug.WriteLine(this, $"Connecting to {_clientSocket.Url}");
            _clientSocket.Connect();
        }

        public ISerializationBinder Binder { get { return _serializer.SerializationBinder; }  set { _serializer.SerializationBinder = value; } }

        private void _clientSocket_OnError(object sender, WebSocketSharp.ErrorEventArgs e)
        {
            Debug.WriteLine(e.Exception);
        }

        private void _clientSocket_OnClose(object sender, CloseEventArgs e)
        {
            Disconnected?.Invoke(this, e);
        }

        public bool IsConnected { get { return _clientSocket.ReadyState == WebSocketState.Open; } }

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
            switch (message.MessageType)
            {
                case WebSocketMessage.WebSocketMessageType.EventNotification:
                    EventNotification?.Invoke(this, new WebSocketMessageEventArgs(message));
                    break;
                case WebSocketMessage.WebSocketMessageType.ObjectDisposed:
                    _referenceResolver.RemoveReference(message.DtoGuid);
                    break;
                default:
                    _receivedMessages[message.MessageGuid] = message;
                    _messageHandler.Set();
                    break;
            }
        }

        private void _clientSocket_OnOpen(object sender, EventArgs e)
        {
            Debug.WriteLine(this, "Connected");
            Connected?.Invoke(this, EventArgs.Empty);
        }

        private WebSocketMessage WaitForResponse(WebSocketMessage sendedMessage)
        {
            Func<WebSocketMessage> resultFunc = new Func<WebSocketMessage>(() =>
            {
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
            });
            IAsyncResult funcAsyncResult = resultFunc.BeginInvoke(null, null);
            funcAsyncResult.AsyncWaitHandle.WaitOne();
            if (funcAsyncResult.IsCompleted)
                return resultFunc.EndInvoke(funcAsyncResult);
            else return null;
        }

        private T _send<T>(WebSocketMessage query)
        {
            if (_clientSocket.ReadyState == WebSocketState.Open)
            {
                _clientSocket.Send(Serialize(query));
                return MethodParametersAlignment.AlignType<T>(WaitForResponse(query).Response);
            }
            else
                return default(T);
        }

        public T GetInitalObject<T>()
        {
            WebSocketMessage query = new WebSocketMessage() { MessageType = WebSocketMessage.WebSocketMessageType.RootQuery };
            return _send<T>(query);
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
            return _send<T>(query);
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
            return _send<T>(query);
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
            if (_clientSocket.ReadyState == WebSocketState.Open)
                _clientSocket.SendAsync(Serialize(query), null);
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
            if (_clientSocket.ReadyState == WebSocketState.Open)
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
            if (_clientSocket.ReadyState == WebSocketState.Open)
                _clientSocket.SendAsync(Serialize(query), null);
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
            if (_clientSocket.ReadyState == WebSocketState.Open)
                _clientSocket.SendAsync(Serialize(query), null);
        }

        private bool _disposed = false;
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _referenceResolver.Dispose();
                _clientSocket.OnOpen -= _clientSocket_OnOpen;
                _clientSocket.OnClose -= _clientSocket_OnClose;
                _clientSocket.OnMessage -= _clientSocket_OnMessage;
                _clientSocket.OnError -= _clientSocket_OnError;
                _clientSocket.Close(CloseStatusCode.Normal);
            }
        }
    }


}
