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
using Newtonsoft.Json.Serialization;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Remoting.Server
{
    public class ServerSession : WebSocketBehavior
    {
        private readonly JsonSerializer _serializer;
        private readonly ReferenceResolver _referenceResolver;
        private readonly ConcurrentDictionary<Tuple<Guid, string>, Delegate> _delegates;

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
        public ISerializationBinder Binder { get { return _serializer.SerializationBinder; } set { _serializer.SerializationBinder = value; } }

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
                        throw new ApplicationException(string.Format("Server: unknown DTO: {0} on {1}", message.DtoGuid, message));
                    }
                }
            }
            catch (Exception ex)
            {
                message.MessageType = WebSocketMessage.WebSocketMessageType.Exception;
                //SerializeDto(message, ex);
                //Send(message.Serialize());
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
                    Delegate delegateToRemove;
                    if (_delegates.TryRemove(d, out delegateToRemove))
                        ei.RemoveEventHandler(havingDelegate, delegateToRemove);
                }
            }
            _referenceResolver.ReferencePropertyChanged -= _referenceResolver_ReferencePropertyChanged;
            _referenceResolver.ReferenceDisposed -= _referencedObjectDisposed;
            _referenceResolver.Dispose();
            Debug.WriteLine("Server: connection closed.");
            base.OnClose(e);
        }

        protected override void OnOpen()
        {
            base.OnOpen();
            Debug.WriteLine("Server: connection open.");
        }

        protected override void OnError(WebSocketSharp.ErrorEventArgs e)
        {
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
            Delegate delegateToRemove;
            if (_delegates.TryRemove(signature, out delegateToRemove))
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

        private void _alignContentTypes(ref object[] inputArray, Type[] inputTypes)
        {
            for (int i = 0; i < inputArray.Length; i++)
            {
                object input = inputArray[i];
                Type requiredType = inputTypes[i];
                if (input != null)
                {
                    Type actualType = input.GetType();
                    if (actualType != requiredType)
                    {
                        MethodParametersAlignment.AlignType(ref input, requiredType);
                        inputArray[i] = input;
                    }
                }
            }
        }

        private void _notifyClient(object o, EventArgs e, string eventName)
        {
            IDto dto = o as IDto;
            if (dto == null)
                return;
            EventArgs eventArgs;
            PropertyChangedEventArgs ea = e as PropertyChangedEventArgs;
            if (ea != null && eventName == nameof(INotifyPropertyChanged.PropertyChanged))
            {
                PropertyInfo p = o.GetType().GetProperty(ea.PropertyName);
                if (p?.CanRead == true)
                    eventArgs = PropertyChangedWithValueEventArgs.Create(ea.PropertyName, p.GetValue(o, null));
                else
                {
                    eventArgs = PropertyChangedWithDataEventArgs.Create(ea.PropertyName, null);
                    Debug.WriteLine(o, $"{GetType()}: Couldn't get value of {ea.PropertyName}");
                }
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
            using (var serialized = SerializeDto(eventArgs))
            {
                var bytes = message.ToByteArray(serialized);
                Send(bytes);
            }
            //Debug.WriteLine($"Server: Notification {eventName} on {dto} sent:\n{s}");
        }

        private void _referencedObjectDisposed(object o, EventArgs a)
        {
            IDto dto = o as IDto;
            if (dto == null)
                return;
            var delegatesToRemove = _delegates.Keys.Where(k => k.Item1 == dto.DtoGuid);
            foreach (var dk in delegatesToRemove)
            {
                Delegate delegateToRemove;
                if (_delegates.TryRemove(dk, out delegateToRemove))
                {
                    EventInfo ei = dto.GetType().GetEvent(dk.Item2);
                    ei.RemoveEventHandler(dto, delegateToRemove);
                    Debug.WriteLine($"Server: Delegate {dk.Item2}  on {dto} removed;");
                }
            }
            WebSocketMessage message = new WebSocketMessage
            {
                MessageType = WebSocketMessage.WebSocketMessageType.ObjectDisposed,
                DtoGuid = dto.DtoGuid,
#if DEBUG
                DtoName = dto.ToString()
#endif
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
            _notifyClient(sender, e, nameof(INotifyPropertyChanged.PropertyChanged));
        }

    }
}
