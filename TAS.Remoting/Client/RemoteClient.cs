//#undef DEBUG
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Newtonsoft.Json;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json.Serialization;

namespace TAS.Remoting.Client
{
    public class RemoteClient: SocketConnection
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private object _initialObject;
        private readonly Dictionary<Guid, SocketMessage> _receivedMessages = new Dictionary<Guid, SocketMessage>();
        private readonly AutoResetEvent _messageReceivedAutoResetEvent = new AutoResetEvent(false);


        public RemoteClient(string address): base(address, new ClientReferenceResolver())
        {
            ((ClientReferenceResolver)ReferenceResolver).ReferenceFinalized += Resolver_ReferenceFinalized;
            StartThreads();
        }

        protected override void OnDispose()
        {
            base.OnDispose();
            ((ClientReferenceResolver)ReferenceResolver).Dispose();
        }
        
        public ISerializationBinder Binder { set => SetBinder(value); }

        public T GetInitalObject<T>()
        {
            try
            {
                var queryMessage = WebSocketMessageCreate(SocketMessage.SocketMessageType.RootQuery, null, null, 0, null);
                var response = SendAndGetResponse<T>(queryMessage);
                _initialObject = response;
                return response;
            }
            catch (Exception e)
            {
                Logger.Error(e, "From GetInitialObject:");
                throw;
            }
        }

        public T Query<T>(ProxyBase dto, string methodName, params object[] parameters)
        {
            try
            {
                var queryMessage = WebSocketMessageCreate(
                    SocketMessage.SocketMessageType.Query,
                    dto,
                    methodName,
                    parameters.Length,
                    new SocketMessageArrayValue { Value = parameters });
                return SendAndGetResponse<T>(queryMessage);
            }
            catch (Exception e)
            {
                Logger.Error("From Query for {0}: {1}", dto, e);
                throw;
            }
        }

        public T Get<T>(ProxyBase dto, string propertyName)
        {
            try
            {
                var queryMessage = WebSocketMessageCreate(
                    SocketMessage.SocketMessageType.Get,
                    dto,
                    propertyName,
                    0,
                    null
                );
                return SendAndGetResponse<T>(queryMessage);
            }
            catch (Exception e)
            {
                Logger.Error("From Get {0}: {1}", dto, e);
                throw;
            }
        }

        public void Invoke(ProxyBase dto, string methodName, params object[] parameters)
        {
            Send(WebSocketMessageCreate(
                SocketMessage.SocketMessageType.Invoke,
                dto,
                methodName,
                parameters.Length,
                new SocketMessageArrayValue{Value = parameters}));
        }

        public void Set(ProxyBase dto, object value, string propertyName)
        {
            Send(WebSocketMessageCreate(
                SocketMessage.SocketMessageType.Set,
                dto,
                propertyName,
                1,
                value));
        }

        public void EventAdd(ProxyBase dto, string eventName)
        {
            Send(WebSocketMessageCreate(
                SocketMessage.SocketMessageType.EventAdd,
                dto,
                eventName,
                0,
                null));
        }

        public void EventRemove(ProxyBase dto, string eventName)
        {
            Send(WebSocketMessageCreate(
                SocketMessage.SocketMessageType.EventRemove,
                dto,
                eventName,
                0,
                null));
        }

        protected override void OnMessage(SocketMessage message)
        {
            if (message.MessageType != SocketMessage.SocketMessageType.RootQuery && _initialObject == null)
                return;
            switch (message.MessageType)
            {
                case SocketMessage.SocketMessageType.EventNotification:
                    var notifyObject = ((ClientReferenceResolver)ReferenceResolver).ResolveReference(message.DtoGuid);
                    notifyObject?.OnEventNotificationMessage(message);
                    break;
                default:
                    lock (((IDictionary)_receivedMessages).SyncRoot)
                    {
                        _receivedMessages[message.MessageGuid] = message;
                        _messageReceivedAutoResetEvent.Set();
                    }
                    break;
            }
        }

        private SocketMessage WebSocketMessageCreate(SocketMessage.SocketMessageType socketMessageType, IDto dto, string memberName, int paramsCount, object value)
        {
            return new SocketMessage(value)
            {
                MessageType = socketMessageType,
                DtoGuid = dto?.DtoGuid ?? Guid.Empty,
                MemberName = memberName,
                ParametersCount = paramsCount
            };
        }

        private T SendAndGetResponse<T>(SocketMessage query)
        {
            Send(query);
            while (IsConnected)
            {
                _messageReceivedAutoResetEvent.WaitOne(5);
                SocketMessage response;
                lock (((IDictionary) _receivedMessages).SyncRoot)
                {
                    if (_receivedMessages.TryGetValue(query.MessageGuid, out response))
                        _receivedMessages.Remove(query.MessageGuid);
                }
                if (response == null)
                    continue;
                if (response.MessageType == SocketMessage.SocketMessageType.Exception)
                    throw Deserialize<Exception>(response);
                var result = Deserialize<T>(response);
                return result;
            }
            return default(T);
        }

        private void Resolver_ReferenceFinalized(object sender, Common.EventArgs<ProxyBase> e)
        {
            Send(WebSocketMessageCreate(
                SocketMessage.SocketMessageType.ProxyFinalized,
                e.Item,
                string.Empty,
                0,
                null));
        }

        private T Deserialize<T>(SocketMessage message)
        {
            using (var valueStream = message.ValueStream)
            {
                if (valueStream == null)
                    return default(T);
                using (var reader = new StreamReader(valueStream))
                    return (T)Serializer.Deserialize(reader, typeof(T));
            }
        }
    }
}
