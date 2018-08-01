//#undef DEBUG
using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Principal;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using WebSocketSharp;
using WebSocketSharp.Server;
using delegateKey = System.Tuple<System.Guid, string>;
using TAS.Common;
using TAS.Common.Interfaces;


namespace TAS.Remoting.Server
{
    public class ServerSession : WebSocketBehavior
    {
        private readonly JsonSerializer _serializer;
        private readonly ReferenceResolver _referenceResolver;
        private readonly ConcurrentDictionary<Tuple<Guid, string>, Delegate> _delegates;
        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger(nameof(ServerSession));

        public ServerSession()
        {
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
        }

#if DEBUG
        ~ServerSession()
        {
            Debug.WriteLine("Finalized: {0} for {1}", this, InitialObject);
        }
#endif

        public IDto InitialObject;
        public IAuthenticationService AuthenticationService;

        protected override void OnMessage(MessageEventArgs e)
        {
            WebSocketMessage message = new WebSocketMessage(e.RawData);
            try
            {
                var user = AuthenticationService.FindUser(AuthenticationSource.IpAddress, Context?.UserEndPoint.Address.ToString());
                if (user == null)
                    throw new UnauthorizedAccessException($"Access from {Context?.UserEndPoint.Address} not allowed");
                Thread.CurrentPrincipal = new GenericPrincipal(user, new string[0]);

                if (message.MessageType == WebSocketMessage.WebSocketMessageType.RootQuery)
                {
                    _sendResponse(message, InitialObject);
                }
                else // method of particular object
                {
                    IDto objectToInvoke = _referenceResolver.ResolveReference(message.DtoGuid);
                    if (objectToInvoke != null)
                    {
                        if (message.MessageType == WebSocketMessage.WebSocketMessageType.Query
                            || message.MessageType == WebSocketMessage.WebSocketMessageType.Invoke)
                        {
                            Type objectToInvokeType = objectToInvoke.GetType();
                            MethodInfo methodToInvoke = objectToInvokeType.GetMethods()
                                .FirstOrDefault(m => m.Name == message.MemberName &&
                                                     m.GetParameters().Length == message.ValueCount);
                            if (methodToInvoke != null)
                            {
                                var parameters = DeserializeDto<WebSocketMessageArrayValue>(message.GetValueStream());
                                ParameterInfo[] methodParameters = methodToInvoke.GetParameters();
                                for (int i = 0; i < methodParameters.Length; i++)
                                    MethodParametersAlignment.AlignType(ref parameters.Value[i],
                                        methodParameters[i].ParameterType);
                                object response = methodToInvoke.Invoke(objectToInvoke,
                                    BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.Public, null,
                                    parameters.Value, null);
                                if (message.MessageType == WebSocketMessage.WebSocketMessageType.Query)
                                    _sendResponse(message, response);
                            }
                            else
                                throw new ApplicationException(
                                    $"Server: unknown method: {objectToInvoke}:{message.MemberName}");
                        }
                        else if (message.MessageType == WebSocketMessage.WebSocketMessageType.Get
                                 || message.MessageType == WebSocketMessage.WebSocketMessageType.Set)
                        {
                            PropertyInfo property = objectToInvoke.GetType().GetProperty(message.MemberName);
                            if (property != null)
                            {
                                if (message.MessageType == WebSocketMessage.WebSocketMessageType.Get &&
                                    property.CanRead)
                                {
                                    object response = property.GetValue(objectToInvoke, null);
                                    _sendResponse(message, response);
                                }
                                else // Set
                                {
                                    if (property.CanWrite)
                                    {
                                        var parameter = DeserializeDto<object>(message.GetValueStream());
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
                        else if (message.MessageType == WebSocketMessage.WebSocketMessageType.EventAdd
                                 || message.MessageType == WebSocketMessage.WebSocketMessageType.EventRemove)
                        {
                            EventInfo ei = objectToInvoke.GetType().GetEvent(message.MemberName);
                            if (ei != null)
                            {
                                if (message.MessageType == WebSocketMessage.WebSocketMessageType.EventAdd)
                                    _addDelegate(objectToInvoke, ei);
                                else if (message.MessageType == WebSocketMessage.WebSocketMessageType.EventRemove)
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
                message.MessageType = WebSocketMessage.WebSocketMessageType.Exception;
                //SerializeDto(message, ex);
                //Send(message.Serialize());
                Logger.Error(ex);
                Debug.WriteLine(ex);
            }
        }

        protected override void OnClose(CloseEventArgs e)
        {
            foreach (delegateKey d in _delegates.Keys)
            {
                IDto havingDelegate = _referenceResolver.ResolveReference(d.Item1);
                if (havingDelegate != null)
                {
                    EventInfo ei = havingDelegate.GetType().GetEvent(d.Item2);
                    _removeDelegate(havingDelegate, ei);
                }
            }
            _referenceResolver.ReferencePropertyChanged -= _referenceResolver_ReferencePropertyChanged;
            _referenceResolver.ReferenceDisposed -= _referencedObjectDisposed;
            _referenceResolver.Dispose();
            Debug.WriteLine("Server: connection closed.");
            Logger.Info("Connection closed.");
            base.OnClose(e);
        }

        protected override void OnOpen()
        {
            base.OnOpen();
            Logger.Info($"Connection open from {Context?.UserEndPoint.Address}");
            Debug.WriteLine("Server: connection open.");
        }

        protected override void OnError(WebSocketSharp.ErrorEventArgs e)
        {
            Logger.Error(e.Exception, e.Message);
            Debug.WriteLine(e.Exception);
            base.OnError(e);
        }

        private void _sendResponse(WebSocketMessage message, object response)
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
            WebSocketMessage message = new WebSocketMessage { 
                MessageType = WebSocketMessage.WebSocketMessageType.EventNotification,
                DtoGuid = dto.DtoGuid,
#if DEBUG
                DtoName = dto.ToString(),
#endif
                MemberName = eventName};
            if (ConnectionState == WebSocketState.Open)
                using (var serialized = SerializeDto(eventArgs))
                {
                    var bytes = message.ToByteArray(serialized);
                    SendAsync(bytes, null);
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
            WebSocketMessage message = new WebSocketMessage
            {
                MessageType = WebSocketMessage.WebSocketMessageType.ObjectDisposed,
                DtoGuid = dto.DtoGuid,
#if DEBUG
                DtoName = dto.ToString()
#endif
            };
            if (ConnectionState == WebSocketState.Open)
                using (var serialized = SerializeDto(null))
                {
                    var bytes = message.ToByteArray(serialized);
                    SendAsync(bytes, null);
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
