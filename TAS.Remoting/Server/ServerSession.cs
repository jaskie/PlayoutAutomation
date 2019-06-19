//#undef DEBUG
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Principal;
using System.Threading;
using TAS.Common;
using TAS.Common.Interfaces.Security;
using System.Collections;

namespace TAS.Remoting.Server
{
    public class ServerSession : SocketConnection
    {
        private readonly Dictionary<DelegateKey, Delegate> _delegates = new Dictionary<DelegateKey, Delegate>();
        private readonly IUser _sessionUser;
        private readonly IDto _initialObject;
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();


        public ServerSession(TcpClient client, IAuthenticationService authenticationService, IDto initialObject): base(client, new ServerReferenceResolver())
        {
            _initialObject = initialObject;
           
            if (!(client.Client.RemoteEndPoint is IPEndPoint endPoint))
                throw new UnauthorizedAccessException($"Client RemoteEndpoint {Client.Client.RemoteEndPoint} is invalid");
            _sessionUser = authenticationService.FindUser(AuthenticationSource.IpAddress, endPoint.Address.ToString());
            if (_sessionUser == null)
                throw new UnauthorizedAccessException($"Access from {Client.Client.RemoteEndPoint} not allowed");
            ((ServerReferenceResolver)ReferenceResolver).ReferencePropertyChanged += ReferenceResolver_ReferencePropertyChanged;
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

        protected override void WriteThreadProc()
        {
            Thread.CurrentPrincipal = new GenericPrincipal(_sessionUser, new string[0]);
            base.WriteThreadProc();
        }


        protected override void OnMessage(SocketMessage message)
        {
            try
            {
                if (message.MessageType == SocketMessage.SocketMessageType.RootQuery)
                {
                    SendResponse(message, _initialObject);
                }
                else // method of particular object
                {
                    var objectToInvoke = ((ServerReferenceResolver)ReferenceResolver).ResolveReference(message.DtoGuid);
                    if (objectToInvoke != null)
                    {
                        Debug.WriteLine($"{objectToInvoke}:{message.MemberName}");
                        switch (message.MessageType)
                        {
                            case SocketMessage.SocketMessageType.Query:
                            case SocketMessage.SocketMessageType.Invoke:
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
                                break;
                            case SocketMessage.SocketMessageType.Get:
                                var getProperty = objectToInvoke.GetType().GetProperty(message.MemberName);
                                if (getProperty != null)
                                {
                                    object response;
                                    try
                                    {
                                        response = getProperty.GetValue(objectToInvoke, null);
                                    }
                                    catch (Exception e)
                                    {
                                        SendException(message, e);
                                        throw;
                                    }
                                    SendResponse(message, response);
                                }
                                else
                                    throw new ApplicationException(
                                        $"Server: unknown property: {objectToInvoke}:{message.MemberName}");
                                break;
                            case SocketMessage.SocketMessageType.Set:
                                var setProperty = objectToInvoke.GetType().GetProperty(message.MemberName);
                                if (setProperty != null)
                                {
                                    var parameter = DeserializeDto<object>(message.ValueStream);
                                    MethodParametersAlignment.AlignType(ref parameter, setProperty.PropertyType);
                                    try
                                    {
                                        setProperty.SetValue(objectToInvoke, parameter, null);
                                    }
                                    catch (Exception e)
                                    {
                                        SendException(message, e);
                                        throw;
                                    }
                                }
                                else
                                    throw new ApplicationException(
                                        $"Server: unknown property: {objectToInvoke}:{message.MemberName}");
                                break;
                            case SocketMessage.SocketMessageType.EventAdd:
                            case SocketMessage.SocketMessageType.EventRemove:
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
                                break;
                            case SocketMessage.SocketMessageType.ProxyFinalized:
                                RemoveReference(objectToInvoke);
                                break;
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
            ((ServerReferenceResolver)ReferenceResolver).ReferencePropertyChanged -= ReferenceResolver_ReferencePropertyChanged;
            lock (((IDictionary) _delegates).SyncRoot)
            {
                foreach (var d in _delegates.Keys.ToArray())
                {
                    var havingDelegate = ((ServerReferenceResolver)ReferenceResolver).ResolveReference(d.DtoGuid);
                    if (havingDelegate == null)
                        throw new ApplicationException("Referenced object not found");
                    var ei = havingDelegate.GetType().GetEvent(d.EventName);
                    RemoveDelegate(havingDelegate, ei);
                }
            }
            ((ServerReferenceResolver)ReferenceResolver).Dispose();
        }

        private void SendResponse(SocketMessage message, object response)
        {
            Send(new SocketMessage(message, response));
        }


        private void AddDelegate(IDto objectToInvoke, EventInfo ei)
        {
            var signature = new DelegateKey(objectToInvoke.DtoGuid, ei.Name);
            lock (((IDictionary) _delegates).SyncRoot)
            {
                if (_delegates.ContainsKey(signature))
                    return;
                var delegateToInvoke = ConvertDelegate((Action<object, EventArgs>) delegate(object o, EventArgs ea) { NotifyClient(objectToInvoke, ea, ei.Name); }, ei.EventHandlerType);
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

        private void NotifyClient(IDto dto, EventArgs e, string eventName)
        {
            //Debug.Assert(_referenceResolver.ResolveReference(dto.DtoGuid) != null, "Null reference notified");
            try
            {
                EventArgs eventArgs;
                if (e is WrappedEventArgs ea 
                    && ea.Args is PropertyChangedEventArgs propertyChangedEventArgs
                    && eventName == nameof(INotifyPropertyChanged.PropertyChanged))
                {
                    var p = dto.GetType().GetProperty(propertyChangedEventArgs.PropertyName);
                    if (p?.CanRead == true)
                        eventArgs = PropertyChangedWithDataEventArgs.Create(propertyChangedEventArgs.PropertyName, p.GetValue(dto, null));
                    else
                    {
                        eventArgs = PropertyChangedWithDataEventArgs.Create(propertyChangedEventArgs.PropertyName, null);
                        Debug.WriteLine(dto, $"{GetType()}: Couldn't get value of {propertyChangedEventArgs.PropertyName}");
                    }
                    Debug.WriteLine($"Server: PropertyChanged {propertyChangedEventArgs.PropertyName} on {dto} sent");
                }
                else
                    eventArgs = e;
                var message = new SocketMessage(eventArgs)
                {
                    MessageType = SocketMessage.SocketMessageType.EventNotification,
                    DtoGuid = dto.DtoGuid,
                    MemberName = eventName,
                    
                };
                Send(message);
            }
            catch (Exception exception)
            {
                Logger.Error(exception);
            }
        }

        private void RemoveReference(IDto dto)
        {
            lock(((IDictionary)_delegates).SyncRoot)
            {
                var delegatesToRemove = _delegates.Keys.Where(k => k.DtoGuid == dto.DtoGuid).ToArray();
                foreach (var dk in delegatesToRemove)
                {
                    var ei = dto.GetType().GetEvent(dk.EventName);
                    RemoveDelegate(dto, ei);
                }
            }
            ((ServerReferenceResolver)ReferenceResolver).RemoveReference(dto);
            Debug.WriteLine($"Server: Reference removed: {dto}");
        }


        private void ReferenceResolver_ReferencePropertyChanged(object sender, WrappedEventArgs e)
        {
            NotifyClient(e.Dto, e, nameof(INotifyPropertyChanged.PropertyChanged));
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
                if (obj == null) return false;
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
