#undef DEBUG
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;
using WebSocketSharp;
using Newtonsoft.Json;
using System.Runtime.Serialization;
using System.Text;
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
            _serializer.TypeNameHandling = TypeNameHandling.Objects | TypeNameHandling.Arrays;
#if DEBUG
            _serializer.Formatting = Formatting.Indented;
#endif
            _clientSocket = new WebSocket($"ws://{host}/Engine") { Compression = CompressionMethod.Deflate, WaitTime = TimeSpan.FromMilliseconds(QueryTimeout), NoDelay = true };
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

        public event EventHandler Connected;
        public event EventHandler Disconnected;

        public ISerializationBinder Binder { get { return _serializer.SerializationBinder; }  set { _serializer.SerializationBinder = value; } }

        public bool IsConnected => _clientSocket.ReadyState == WebSocketState.Open;

        public T GetInitalObject<T>()
        {
            WebSocketMessage queryMessage = WebSocketMessageCreate(WebSocketMessage.WebSocketMessageType.RootQuery, null, null, 0);
            return SendAndGetResponse<T>(queryMessage, null);
        }

        public T Query<T>(ProxyBase dto, string methodName, params object[] parameters)
        {
            WebSocketMessage queryMessage = WebSocketMessageCreate(
                WebSocketMessage.WebSocketMessageType.Query,
                dto,
                methodName,
                parameters.Length);
            return SendAndGetResponse<T>(queryMessage, new WebSocketMessageArrayValue {Value = parameters});
        }

        public T Get<T>(ProxyBase dto, string propertyName)
        {
            WebSocketMessage queryMessage = WebSocketMessageCreate(
                WebSocketMessage.WebSocketMessageType.Get,
                dto,
                propertyName,
                0
            );
            return SendAndGetResponse<T>(queryMessage, null);
        }

        public void Invoke(ProxyBase dto, string methodName, params object[] parameters)
        {
            WebSocketMessage queryMessage = WebSocketMessageCreate(
                WebSocketMessage.WebSocketMessageType.Invoke,
                dto,
                methodName,
                parameters.Length);
            if (_clientSocket.ReadyState == WebSocketState.Open)
                using (var valueStream = Serialize(new WebSocketMessageArrayValue { Value = parameters }))
                {
                    _clientSocket.Send(queryMessage.ToByteArray(valueStream));
                }
        }

        public void Set(ProxyBase dto, object value, string propertyName)
        {
            WebSocketMessage queryMessage = WebSocketMessageCreate(
                WebSocketMessage.WebSocketMessageType.Set,
                dto,
                propertyName,
                1);
            if (_clientSocket.ReadyState == WebSocketState.Open)
                using (var valueStream = Serialize(value))
                    _clientSocket.Send(queryMessage.ToByteArray(valueStream));
        }

        public void EventAdd(ProxyBase dto, string eventName)
        {
            WebSocketMessage queryMessage = WebSocketMessageCreate(
                WebSocketMessage.WebSocketMessageType.EventAdd,
                dto,
                eventName,
                0);
            if (_clientSocket.ReadyState == WebSocketState.Open)
                _clientSocket.Send(queryMessage.ToByteArray(null));
        }

        public void EventRemove(ProxyBase dto, string eventName)
        {
            WebSocketMessage queryMessage = WebSocketMessageCreate(
                WebSocketMessage.WebSocketMessageType.EventRemove,
                dto,
                eventName,
                0);
            if (_clientSocket.ReadyState == WebSocketState.Open)
                _clientSocket.Send(queryMessage.ToByteArray(null));
        }


        private void _clientSocket_OnError(object sender, WebSocketSharp.ErrorEventArgs e)
        {
            Debug.WriteLine(e.Exception);
        }

        private void _clientSocket_OnClose(object sender, CloseEventArgs e)
        {
            Disconnected?.Invoke(this, e);
        }

        private Stream Serialize(object o)
        {
            if (o == null)
                return null;
            var stream = new MemoryStream();
            using (var writer = new StreamWriter(stream, Encoding.UTF8, 1024, true))
            {
                _serializer.Serialize(writer, o);
                return stream;
            }
        }

        internal T Deserialize<T>(WebSocketMessage message)
        {
            using (var valueStream = message.GetValueStream())
            {
                if (valueStream != null)
                    using (var reader = new StreamReader(valueStream))
                    using (var jsonReader = new JsonTextReader(reader))
                    {
                        return _serializer.Deserialize<T>(jsonReader);
                    }
            }
            return default(T);
        }

        private void _clientSocket_OnMessage(object sender, MessageEventArgs e)
        {
            WebSocketMessage message = new WebSocketMessage(e.RawData);
            switch (message.MessageType)
            {
                case WebSocketMessage.WebSocketMessageType.EventNotification:
                    var proxy = _referenceResolver.ResolveReference(message.DtoGuid) as ProxyBase;
                    proxy?.OnEventNotificationMessage(message);
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

        private WebSocketMessage WebSocketMessageCreate(WebSocketMessage.WebSocketMessageType webSocketMessageType, IDto dto, string memberName, int paramsCount)
        {
            return new WebSocketMessage
            {
                MessageType = webSocketMessageType,
                DtoGuid = dto?.DtoGuid ?? Guid.Empty,
                MemberName = memberName,
                ValueCount = paramsCount
            };
        }

        private T SendAndGetResponse<T>(WebSocketMessage query, object value)
        {
            if (_clientSocket.ReadyState == WebSocketState.Open)
            {
                using (var valueStream = Serialize(value))
                {
                    _clientSocket.Send(query.ToByteArray(valueStream));
                }
                var response = WaitForResponse(query);

                return Deserialize<T>(response);
            }
            return default(T);
        }
    }
}
