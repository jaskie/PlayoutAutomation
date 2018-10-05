#undef DEBUG
using System;
using System.Collections.Concurrent;
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
using delegateKey = System.Tuple<System.Guid, string>;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Security;


namespace TAS.Remoting.Server
{
    public class ServerSession : TcpConnection
    {
        private readonly JsonSerializer _serializer;
        private readonly ReferenceResolver _referenceResolver;
        private readonly ConcurrentDictionary<Tuple<Guid, string>, Delegate> _delegates;
        private readonly IUser _sessionUser;
        private readonly IDto _initialObject;
        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger(nameof(ServerSession));

        public ServerSession(TcpClient client, IAuthenticationService authenticationService, IDto initialObject): base(client)
        {
            _initialObject = initialObject;
            _delegates = new ConcurrentDictionary<delegateKey, Delegate>();
            _serializer = JsonSerializer.CreateDefault();
            _referenceResolver = new ReferenceResolver();
            _referenceResolver.ReferencePropertyChanged += _referenceResolver_ReferencePropertyChanged;
            _referenceResolver.ReferenceDisposed += _referencedObjectDisposed;
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
                    _sendResponse(message, _initialObject);
                }
                else // method of particular object
                {
                    IDto objectToInvoke = _referenceResolver.ResolveReference(message.DtoGuid);
                    if (objectToInvoke != null)
                    {
                        if (message.MessageType == SocketMessage.SocketMessageType.Query
                            || message.MessageType == SocketMessage.SocketMessageType.Invoke)
                        {
                            Type objectToInvokeType = objectToInvoke.GetType();
                            MethodInfo methodToInvoke = objectToInvokeType.GetMethods()
                                .FirstOrDefault(m => m.Name == message.MemberName &&
                                                     m.GetParameters().Length == message.ValueCount);
                            if (methodToInvoke != null)
                            {
                                var parameters = DeserializeDto<SocketMessageArrayValue>(message.ValueStream);
                                ParameterInfo[] methodParameters = methodToInvoke.GetParameters();
                                for (int i = 0; i < methodParameters.Length; i++)
                                    MethodParametersAlignment.AlignType(ref parameters.Value[i],
                                        methodParameters[i].ParameterType);
                                object response = methodToInvoke.Invoke(objectToInvoke,
                                    BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.Public, null,
                                    parameters.Value, null);
                                if (message.MessageType == SocketMessage.SocketMessageType.Query)
                                    _sendResponse(message, response);
                            }
                            else
                                throw new ApplicationException(
                                    $"Server: unknown method: {objectToInvoke}:{message.MemberName}");
                        }
                        else if (message.MessageType == SocketMessage.SocketMessageType.Get
                                 || message.MessageType == SocketMessage.SocketMessageType.Set)
                        {
                            PropertyInfo property = objectToInvoke.GetType().GetProperty(message.MemberName);
                            if (property != null)
                            {
                                if (message.MessageType == SocketMessage.SocketMessageType.Get &&
                                    property.CanRead)
                                {
                                    object response = property.GetValue(objectToInvoke, null);
                                    _sendResponse(message, response);
                                }
                                else // Set
                                {
                                    if (property.CanWrite)
                                    {
                                        var parameter = DeserializeDto<object>(message.ValueStream);
                                        MethodParametersAlignment.AlignType(ref parameter, property.PropertyType);
                                        property.SetValue(objectToInvoke, parameter, null);
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
                            EventInfo ei = objectToInvoke.GetType().GetEvent(message.MemberName);
                            if (ei != null)
                            {
                                if (message.MessageType == SocketMessage.SocketMessageType.EventAdd)
                                    _addDelegate(objectToInvoke, ei);
                                else if (message.MessageType == SocketMessage.SocketMessageType.EventRemove)
                                    _removeDelegate(objectToInvoke, ei);
                            }
                            else
                                throw new ApplicationException(
                                    $"Server: unknown event: {objectToInvoke}:{message.MemberName}");
                        }
                    }
                    else
                    {
                        _sendResponse(message, null);
                        throw new ApplicationException($"Server: unknown DTO: {message.DtoGuid} on {message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        protected override void OnDispose()
        {
            base.OnDispose();
            foreach (delegateKey d in _delegates.Keys)
            {
                var havingDelegate = _referenceResolver.ResolveReference(d.Item1);
                if (havingDelegate == null)
                    continue;
                var ei = havingDelegate.GetType().GetEvent(d.Item2);
                _removeDelegate(havingDelegate, ei);
            }
            _referenceResolver.ReferencePropertyChanged -= _referenceResolver_ReferencePropertyChanged;
            _referenceResolver.ReferenceDisposed -= _referencedObjectDisposed;
            _referenceResolver.Dispose();
        }

        private void _sendResponse(SocketMessage message, object response)
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

        private void _addDelegate(IDto objectToInvoke, EventInfo ei)
        {
            delegateKey signature = new delegateKey(objectToInvoke.DtoGuid, ei.Name);
            if (_delegates.ContainsKey(signature))
                return;
            Delegate delegateToInvoke = ConvertDelegate((Action<object, EventArgs>)delegate (object o, EventArgs ea) { _notifyClient(o, ea, ei.Name); }, ei.EventHandlerType);
            Debug.WriteLine($"Server: added delegate {ei.Name} on {objectToInvoke}");
            _delegates[signature] = delegateToInvoke;
            ei.AddEventHandler(objectToInvoke, delegateToInvoke);
        }

        private void _removeDelegate(IDto objectToInvoke, EventInfo ei)
        {
            delegateKey signature = new delegateKey(objectToInvoke.DtoGuid, ei.Name);
            if (_delegates.TryRemove(signature, out var delegateToRemove))
            {
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

        private void _notifyClient(object o, EventArgs e, string eventName)
        {
            if (!(o is IDto dto))
                return;
            //Debug.Assert(_referenceResolver.ResolveReference(dto.DtoGuid) != null, "Null reference notified");
            EventArgs eventArgs;
            if (e is PropertyChangedEventArgs ea && eventName == nameof(INotifyPropertyChanged.PropertyChanged))
            {
                PropertyInfo p = o.GetType().GetProperty(ea.PropertyName);
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
            SocketMessage message = new SocketMessage
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

        private void _referencedObjectDisposed(object o, EventArgs a)
        {
            if (!(o is IDto dto))
                return;
            var delegatesToRemove = _delegates.Keys.Where(k => k.Item1 == dto.DtoGuid);
            foreach (var dk in delegatesToRemove)
            {
                EventInfo ei = dto.GetType().GetEvent(dk.Item2);
                _removeDelegate(dto, ei);
            }
            SocketMessage message = new SocketMessage
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

        private void _referenceResolver_ReferencePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            try
            {
                _notifyClient(sender, e, nameof(INotifyPropertyChanged.PropertyChanged));
            }
            catch (Exception exception)
            {
                Logger.Error(exception);
            }
        }

    }
}
