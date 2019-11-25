//#undef DEBUG
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Principal;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using TAS.Common;
using TAS.Common.Interfaces.Security;
using System.Collections;

namespace TAS.Remoting.Server
{
    public class ServerSession : TcpConnection
    {
        private readonly JsonSerializer _serializer = JsonSerializer.CreateDefault();
        private readonly ServerReferenceResolver _referenceResolver = new ServerReferenceResolver();
        private readonly Dictionary<DelegateKey, Delegate> _delegates = new Dictionary<DelegateKey, Delegate>();
        private readonly IUser _sessionUser;
        private readonly IDto _initialObject;
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public ServerSession(TcpClient client, IAuthenticationService authenticationService, IDto initialObject): base(client)
        {
            _initialObject = initialObject;
           
            _serializer.ReferenceResolver = _referenceResolver;
            _serializer.TypeNameHandling = TypeNameHandling.Objects;
            _serializer.Context = new StreamingContext(StreamingContextStates.Remoting);

#if DEBUG
            _serializer.Formatting = Formatting.Indented;
#endif
            if (!(client.Client.RemoteEndPoint is IPEndPoint endPoint))
                throw new UnauthorizedAccessException($"Client RemoteEndpoint {Client.Client.RemoteEndPoint} is invalid");
            _sessionUser = authenticationService.FindUser(AuthenticationSource.IpAddress, endPoint.Address.ToString());
            if (_sessionUser == null)
                throw new UnauthorizedAccessException($"Access from {Client.Client.RemoteEndPoint} not allowed");
            StartThreads();
        }
        

#if DEBUG
        ~ServerSession()
        {
            Debug.WriteLine("Finalized: {0} for {1}", this, _initialObject);
        }
#endif

        protected override void ReadThreadProc()
        {
            Thread.CurrentPrincipal = new GenericPrincipal(_sessionUser, new string[0]);
            base.ReadThreadProc();
        }

        protected override void OnMessage(byte[] data)
        {
            var message = new SocketMessage(data);
            try
            {
                if (message.MessageType == SocketMessage.SocketMessageType.RootQuery)
                {
                    SendResponse(message, _initialObject);
                    _referenceResolver.ReferencePropertyChanged += ReferenceResolver_ReferencePropertyChanged;
                    _referenceResolver.ReferenceDisposed += ReferencedObjectDisposed;
                }
                else // method of particular object
                {
                    var objectToInvoke = _referenceResolver.ResolveReference(message.DtoGuid);
                    if (objectToInvoke != null)
                    {
                        if (message.MessageType == SocketMessage.SocketMessageType.Query
                            || message.MessageType == SocketMessage.SocketMessageType.Invoke)
                        {
                            var objectToInvokeType = objectToInvoke.GetType();
                            var methodToInvoke = objectToInvokeType.GetMethods()
                                .FirstOrDefault(m => m.Name == message.MemberName &&
                                                     m.GetParameters().Length == message.ParametersCount);
                            if (methodToInvoke != null)
                            {
                                var parameters = DeserializeDto<SocketMessageArrayValue>(message.ValueStream);
                                var methodParameters = methodToInvoke.GetParameters();
                                for (var i = 0; i < methodParameters.Length; i++)
                                    MethodParametersAlignment.AlignType(ref parameters.Value[i],
                                        methodParameters[i].ParameterType);
                                object response;
                                try
                                {
                                    response = methodToInvoke.Invoke(objectToInvoke, parameters.Value);
                                }
                                catch (Exception e)
                                {
                                    SendException(message, e);
                                    throw;
                                }
                                if (message.MessageType == SocketMessage.SocketMessageType.Query)
                                    SendResponse(message, response);
                            }
                            else
                                throw new ApplicationException(
                                    $"Server: unknown method: {objectToInvoke}:{message.MemberName}");
                        }
                        else if (message.MessageType == SocketMessage.SocketMessageType.Get
                                 || message.MessageType == SocketMessage.SocketMessageType.Set)
                        {
                            var property = objectToInvoke.GetType().GetProperty(message.MemberName);
                            if (property != null)
                            {
                                if (message.MessageType == SocketMessage.SocketMessageType.Get &&
                                    property.CanRead)
                                {
                                    object response;
                                    try
                                    {
                                        response = property.GetValue(objectToInvoke, null);
                                    }
                                    catch (Exception e)
                                    {
                                        SendException(message, e);
                                        throw;
                                    }
                                    SendResponse(message, response);
                                }
                                else // Set
                                {
                                    if (property.CanWrite)
                                    {
                                        var parameter = DeserializeDto<object>(message.ValueStream);
                                        MethodParametersAlignment.AlignType(ref parameter, property.PropertyType);
                                        try
                                        {
                                            property.SetValue(objectToInvoke, parameter, null);
                                        }
                                        catch (Exception e)
                                        {
                                            SendException(message, e);
                                            throw;
                                        }
                                    }
                                    else
                                        throw new ApplicationException(
                                            $"Server: not writable property: {objectToInvoke}:{message.MemberName}");
                                }
                            }
                            else
                                throw new ApplicationException(
                                    $"Server: unknown property: {objectToInvoke}:{message.MemberName}");
                        }
                        else if (message.MessageType == SocketMessage.SocketMessageType.EventAdd
                                 || message.MessageType == SocketMessage.SocketMessageType.EventRemove)
                        {
                            var ei = objectToInvoke.GetType().GetEvent(message.MemberName);
                            if (ei != null)
                            {
                                if (message.MessageType == SocketMessage.SocketMessageType.EventAdd)
                                    AddDelegate(objectToInvoke, ei);
                                else if (message.MessageType == SocketMessage.SocketMessageType.EventRemove)
                                    RemoveDelegate(objectToInvoke, ei);
                            }
                            else
                                throw new ApplicationException(
                                    $"Server: unknown event: {objectToInvoke}:{message.MemberName}");
                        }
                    }
                    else
                    {
                        SendResponse(message, null);
                        throw new ApplicationException($"Server: unknown DTO: {message.DtoGuid} on {message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private void SendException(SocketMessage message, Exception exception)
        {
            message.MessageType = SocketMessage.SocketMessageType.Exception;
            SendResponse(message, new Exception(exception.Message, exception.InnerException == null ? null : new Exception(exception.InnerException.Message)));
        }

        protected override void OnDispose()
        {
            base.OnDispose();
            _referenceResolver.ReferencePropertyChanged -= ReferenceResolver_ReferencePropertyChanged;
            _referenceResolver.ReferenceDisposed -= ReferencedObjectDisposed;
            lock (((IDictionary) _delegates).SyncRoot)
            {
                foreach (var d in _delegates.Keys.ToArray())
                {
                    var havingDelegate = _referenceResolver.ResolveReference(d.DtoGuid);
                    if (havingDelegate == null)
                        throw new ApplicationException("Referenced object not found");
                    var ei = havingDelegate.GetType().GetEvent(d.EventName);
                    RemoveDelegate(havingDelegate, ei);
                }
            }
            _referenceResolver.Dispose();
        }

        private void SendResponse(SocketMessage message, object response)
        {
            using (var serialized = SerializeDto(response))
            {
                var bytes = message.ToByteArray(serialized);
                Send(bytes);
            }
        }

        private Stream SerializeDto(object response)
        {
            if (response == null)
                return null;
            var serialized = new MemoryStream();
            using (var writer = new StreamWriter(serialized, Encoding.UTF8, 1024, true))
                _serializer.Serialize(writer, response);
            return serialized;
        }

        private T DeserializeDto<T>(Stream stream)
        {
            if (stream == null)
                return default(T);
            using (var reader = new StreamReader(stream))
            {
                return (T)_serializer.Deserialize(reader, typeof(T));
            }
        }

        private void AddDelegate(IDto objectToInvoke, EventInfo ei)
        {
            var signature = new DelegateKey(objectToInvoke.DtoGuid, ei.Name);
            lock (((IDictionary) _delegates).SyncRoot)
            {
                if (_delegates.ContainsKey(signature))
                    return;
                var delegateToInvoke = ConvertDelegate((Action<object, EventArgs>) delegate(object o, EventArgs ea) { NotifyClient(o, ea, ei.Name); }, ei.EventHandlerType);
                Debug.WriteLine($"Server: added delegate {ei.Name} on {objectToInvoke}");
                _delegates[signature] = delegateToInvoke;
                ei.AddEventHandler(objectToInvoke, delegateToInvoke);
            }
        }

        private void RemoveDelegate(IDto objectToInvoke, EventInfo ei)
        {
            var signature = new DelegateKey(objectToInvoke.DtoGuid, ei.Name);
            lock (((IDictionary) _delegates).SyncRoot)
            {
                var delegateToRemove = _delegates[signature];
                if (!_delegates.Remove(signature))
                    return;
                ei.RemoveEventHandler(objectToInvoke, delegateToRemove);
                Debug.WriteLine($"Server: removed delegate {ei.Name} on {objectToInvoke}");
            }
        }

        private static Delegate ConvertDelegate(Delegate originalDelegate, Type targetDelegateType)
        {
            return Delegate.CreateDelegate(
                targetDelegateType,
                originalDelegate.Target,
                originalDelegate.Method);
        }

        private void NotifyClient(object o, EventArgs e, string eventName)
        {
            if (!(o is IDto dto))
                return;
            //Debug.Assert(_referenceResolver.ResolveReference(dto.DtoGuid) != null, "Null reference notified");
            EventArgs eventArgs;
            if (e is PropertyChangedEventArgs ea && eventName == nameof(INotifyPropertyChanged.PropertyChanged))
            {
                var p = o.GetType().GetProperty(ea.PropertyName);
                if (p?.CanRead == true)
                    eventArgs = PropertyChangedWithDataEventArgs.Create(ea.PropertyName, p.GetValue(o, null));
                else
                {
                    eventArgs = PropertyChangedWithDataEventArgs.Create(ea.PropertyName, null);
                    Debug.WriteLine(o, $"{GetType()}: Couldn't get value of {ea.PropertyName}");
                }
                Debug.WriteLine($"Server: PropertyChanged {ea.PropertyName} on {dto} sent");
            }
            else
                eventArgs = e;
            var message = new SocketMessage
            {
                MessageType = SocketMessage.SocketMessageType.EventNotification,
                DtoGuid = dto.DtoGuid,
                MemberName = eventName
            };
            using (var serialized = SerializeDto(eventArgs))
            {
                var bytes = message.ToByteArray(serialized);
                Send(bytes);
            }
        }

        private void ReferencedObjectDisposed(object o, EventArgs a)
        {
            if (!(o is IDto dto))
                return;
            lock (((IDictionary) _delegates).SyncRoot)
            {
                var delegatesToRemove = _delegates.Keys.Where(k => k.DtoGuid == dto.DtoGuid).ToArray();
                foreach (var dk in delegatesToRemove)
                {
                    var ei = dto.GetType().GetEvent(dk.EventName);
                    RemoveDelegate(dto, ei);
                }
            }
            var message = new SocketMessage
            {
                MessageType = SocketMessage.SocketMessageType.ObjectDisposed,
                DtoGuid = dto.DtoGuid,
            };
            using (var serialized = SerializeDto(null))
            {
                var bytes = message.ToByteArray(serialized);
                Send(bytes);
            }
            Debug.WriteLine($"Server: ObjectDisposed notification on {dto} sent");
        }

        private void ReferenceResolver_ReferencePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            try
            {
                NotifyClient(sender, e, nameof(INotifyPropertyChanged.PropertyChanged));
            }
            catch (Exception exception)
            {
                Logger.Error(exception);
            }
        }

        private class DelegateKey
        {
            public DelegateKey(Guid dtoGuid, string eventName)
            {
                DtoGuid = dtoGuid;
                EventName = eventName;
            }
            public Guid DtoGuid { get; }
            public string EventName { get; }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                return obj.GetType() == typeof(DelegateKey) && Equals((DelegateKey)obj);
            }

            private bool Equals(DelegateKey other)
            {
                return DtoGuid.Equals(other.DtoGuid) && string.Equals(EventName, other.EventName);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (DtoGuid.GetHashCode() * 397) ^ (EventName != null ? EventName.GetHashCode() : 0);
                }
            }
        }

    }
}
