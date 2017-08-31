//#undef DEBUG
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;
using WebSocketSharp;
using Newtonsoft.Json;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Newtonsoft.Json.Serialization;

namespace TAS.Remoting.Client
{
    public class RemoteClient: IDisposable
    {
        private readonly WebSocket _clientSocket;
        private readonly AutoResetEvent _messageHandler = new AutoResetEvent(false);
        private readonly JsonSerializer _serializer;
        private readonly ReferenceResolver _referenceResolver;
        private readonly ConcurrentDictionary<Guid, WebSocketMessage> _receivedMessages = new ConcurrentDictionary<Guid, WebSocketMessage>();
        private readonly BinaryFormatter _messageFormatter = new BinaryFormatter();


        private const int QueryTimeout =
#if DEBUG 
            50000
#else
            3000
#endif
            ;
        private int _disposed;


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
            _clientSocket = new WebSocket($"ws://{host}/Engine") { Compression = CompressionMethod.Deflate, WaitTime = TimeSpan.FromMilliseconds(QueryTimeout) };
            _clientSocket.OnOpen += _clientSocket_OnOpen;
            _clientSocket.OnClose += _clientSocket_OnClose;
            _clientSocket.OnMessage += _clientSocket_OnMessage;
            _clientSocket.OnError += _clientSocket_OnError;
            Debug.WriteLine(this, $"Connecting to {_clientSocket.Url}");
            _clientSocket.Connect();
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == default(int))
            {
                _referenceResolver.Dispose();
                _clientSocket.OnOpen -= _clientSocket_OnOpen;
                _clientSocket.OnClose -= _clientSocket_OnClose;
                _clientSocket.OnMessage -= _clientSocket_OnMessage;
                _clientSocket.OnError -= _clientSocket_OnError;
                _clientSocket.Close(CloseStatusCode.Normal);
            }
        }

        public event EventHandler<WebSocketMessageEventArgs> EventNotification;
        public event EventHandler Connected;
        public event EventHandler Disconnected;

        public ISerializationBinder Binder { get { return _serializer.SerializationBinder; }  set { _serializer.SerializationBinder = value; } }

        public bool IsConnected => _clientSocket.ReadyState == WebSocketState.Open;

        public T GetInitalObject<T>()
        {
            WebSocketMessage query = WebSocketMessageCreate(WebSocketMessage.WebSocketMessageType.RootQuery, null, null);
            return _send<T>(query);
        }

        public T Query<T>(ProxyBase dto, string methodName, params object[] parameters)
        {
            WebSocketMessage query = WebSocketMessageCreate(
                WebSocketMessage.WebSocketMessageType.Query,
                dto,
                methodName,
                parameters);
            return _send<T>(query);
        }

        public T Get<T>(ProxyBase dto, string propertyName)
        {
            WebSocketMessage query = WebSocketMessageCreate(
                WebSocketMessage.WebSocketMessageType.Get,
                dto,
                propertyName
            );
            return _send<T>(query);
        }

        public void Invoke(ProxyBase dto, string methodName, params object[] parameters)
        {
            WebSocketMessage query = WebSocketMessageCreate(
                WebSocketMessage.WebSocketMessageType.Invoke,
                dto,
                methodName,
                parameters);
            if (_clientSocket.ReadyState == WebSocketState.Open)
                _clientSocket.SendAsync(Serialize(query), null);
        }

        public void Set(ProxyBase dto, object value, string propertyName)
        {
            WebSocketMessage query = WebSocketMessageCreate(
                WebSocketMessage.WebSocketMessageType.Set,
                dto,
                propertyName,
                value);
            if (_clientSocket.ReadyState == WebSocketState.Open)
                _clientSocket.Send(Serialize(query));
        }

        public void EventAdd(ProxyBase dto, string eventName)
        {
            WebSocketMessage query = WebSocketMessageCreate(
                WebSocketMessage.WebSocketMessageType.EventAdd,
                dto,
                eventName);
            if (_clientSocket.ReadyState == WebSocketState.Open)
                _clientSocket.SendAsync(Serialize(query), null);
        }

        public void EventRemove(ProxyBase dto, string eventName)
        {
            WebSocketMessage query = WebSocketMessageCreate(
                WebSocketMessage.WebSocketMessageType.EventRemove,
                dto,
                eventName);
            if (_clientSocket.ReadyState == WebSocketState.Open)
                _clientSocket.SendAsync(Serialize(query), null);
        }


        private void _clientSocket_OnError(object sender, WebSocketSharp.ErrorEventArgs e)
        {
            Debug.WriteLine(e.Exception);
        }

        private void _clientSocket_OnClose(object sender, CloseEventArgs e)
        {
            Disconnected?.Invoke(this, e);
        }

        private string Serialize(object o)
        {
            using (var writer = new StringWriter())
            {
                _serializer.Serialize(writer, o);
                return writer.ToString();
            }
        }

        private void _clientSocket_OnMessage(object sender, MessageEventArgs e)
        {
            Debug.WriteLine(e.Data);
            WebSocketMessage message;
            message = WebSocketMessage.Deserialize(_messageFormatter, e.RawData);

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
            Func<WebSocketMessage> resultFunc = () =>
            {
                WebSocketMessage response;
                Stopwatch timeout = Stopwatch.StartNew();
                if (_receivedMessages.TryRemove(sendedMessage.MessageGuid, out response))
                    return response;
                do
                {
                    _messageHandler.WaitOne(QueryTimeout);
                    if (_receivedMessages.TryRemove(sendedMessage.MessageGuid, out response))
                        return response;
                }
                while (timeout.ElapsedMilliseconds < QueryTimeout);
                throw new TimeoutException($"Didn't received response from server within {QueryTimeout} milliseconds. Query was {sendedMessage}");
            };
            IAsyncResult funcAsyncResult = resultFunc.BeginInvoke(null, null);
            funcAsyncResult.AsyncWaitHandle.WaitOne();
            if (funcAsyncResult.IsCompleted)
                return resultFunc.EndInvoke(funcAsyncResult);
            return null;
        }

        private WebSocketMessage WebSocketMessageCreate(WebSocketMessage.WebSocketMessageType webSocketMessageType, IDto dto, string memberName, params object[] parameters)
        {
            return new WebSocketMessage
            {
                MessageType = webSocketMessageType,
                DtoGuid = dto?.DtoGuid ?? Guid.Empty,
                MemberName = memberName
            };
        }

        private T _send<T>(WebSocketMessage query)
        {
            if (_clientSocket.ReadyState == WebSocketState.Open)
            {
                _clientSocket.Send(query.Serialize(_messageFormatter));
                using (var reader = new StringReader(WaitForResponse(query).Value))
                {
                    return (T)_serializer.Deserialize(reader, typeof(T));
                }
            }
            return default(T);
        }
    }
}
