using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebSocketSharp;
using WebSocketSharp.Server;
using delegateKey = System.Tuple<System.Guid, string>;

namespace TAS.Remoting.Server
{
    public class CommunicationBehavior : WebSocketBehavior
    {
        readonly JsonSerializer _serializer;
        readonly IDto _initialObject;
        readonly ReferenceResolver _referenceResolver;
        //readonly ServerSerializationConverter _converter;
        public CommunicationBehavior(IDto initialObject)
        {
            _initialObject = initialObject;
            _delegates = new ConcurrentDictionary<delegateKey, Delegate>();
            Debug.WriteLine(initialObject, "Server: created behavior for");
            _serializer = JsonSerializer.CreateDefault();
            _referenceResolver = new ReferenceResolver();
            _referenceResolver.ReferencePropertyChanged += _referenceResolver_ReferencePropertyChanged;
            _serializer.ReferenceResolver = _referenceResolver;
            _serializer.TypeNameHandling = TypeNameHandling.None;
            _serializer.PreserveReferencesHandling = PreserveReferencesHandling.Objects;
            //_converter = new ServerSerializationConverter();
            //_serializer.Converters.Add(_converter);
        }

        private void _referenceResolver_ReferencePropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            _notifyClient(sender, e, nameof(INotifyPropertyChanged.PropertyChanged));
        }

        public SerializationBinder Binder { get { return _serializer.Binder; } set { _serializer.Binder = value; } }

#if DEBUG
        ~CommunicationBehavior()
        {
            Debug.WriteLine("Finalized: {0} for {1}", this, _initialObject);
        }
#endif

        protected ConcurrentDictionary<Tuple<Guid, string>, Delegate> _delegates;

        protected override void OnMessage(MessageEventArgs e)
        {
            WebSocketMessage message = Deserialize<WebSocketMessage>(e.Data);
            try
            {
                if (message.MessageType == WebSocketMessage.WebSocketMessageType.RootQuery)
                    SendResponse(message, _initialObject);
                else // method of particular object
                {
                    IDto objectToInvoke = _referenceResolver.ResolveReference(message.DtoGuid);
                    if (objectToInvoke != null)
                    { 
                        if (message.MessageType == WebSocketMessage.WebSocketMessageType.Query
                            || message.MessageType == WebSocketMessage.WebSocketMessageType.Invoke)
                        {
                            Type objectToInvokeType = objectToInvoke.GetType();
                            MethodInfo methodToInvoke = objectToInvokeType.GetMethods().FirstOrDefault(m => m.Name == message.MemberName && m.GetParameters().Length == message.Parameters.Length);
                            if (methodToInvoke != null)
                            {
                                ParameterInfo[] methodParameters = methodToInvoke.GetParameters();
                                _deserializeContent(ref message.Parameters, methodParameters.Select(p => p.ParameterType).ToArray());
                                object response = methodToInvoke.Invoke(objectToInvoke, BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.Public, null, message.Parameters, null);
                                if (message.MessageType == WebSocketMessage.WebSocketMessageType.Query)
                                    SendResponse(message, response);
                            }
                            else
                                throw new ApplicationException(string.Format("Server: unknown method: {0}:{1}", objectToInvoke, message.MemberName));
                        }
                        else
                        if (message.MessageType == WebSocketMessage.WebSocketMessageType.Get
                            || message.MessageType == WebSocketMessage.WebSocketMessageType.Set)
                        {
                            PropertyInfo property = objectToInvoke.GetType().GetProperty(message.MemberName);
                            if (property != null)
                            {
                                if (message.MessageType == WebSocketMessage.WebSocketMessageType.Get && property.CanRead)
                                {
                                    object response = property.GetValue(objectToInvoke, null);
                                    SendResponse(message, response);
                                }
                                else // Set
                                {
                                    if (property.CanWrite)
                                    {
                                        _deserializeContent(ref message.Parameters, new Type[] { property.PropertyType });
                                        property.SetValue(objectToInvoke, message.Parameters[0], null);
                                    }
                                    else
                                        throw new ApplicationException(string.Format("Server: not writable property: {0}:{1}", objectToInvoke, message.MemberName));
                                }
                            }
                            else
                                throw new ApplicationException(string.Format("Server: unknown property: {0}:{1}", objectToInvoke, message.MemberName));
                        }
                        else
                        if (message.MessageType == WebSocketMessage.WebSocketMessageType.EventAdd
                            || message.MessageType == WebSocketMessage.WebSocketMessageType.EventRemove)
                        {
                            EventInfo ei = objectToInvoke.GetType().GetEvent(message.MemberName);
                            if (ei != null)
                            {
                                if (message.MessageType == WebSocketMessage.WebSocketMessageType.EventAdd)
                                    _addDelegate(objectToInvoke, ei);
                                else
                                if (message.MessageType == WebSocketMessage.WebSocketMessageType.EventRemove)
                                    _removeDelegate(objectToInvoke, ei);
                            }
                            else
                                throw new ApplicationException(string.Format("Server: unknown event: {0}:{1}", objectToInvoke, message.MemberName));
                        }
                    }
                    else
                        throw new ApplicationException(string.Format("Server: unknown DTO: {0} on {1}", message.DtoGuid, message));
                }
            }
            catch (Exception ex)
            {
                message.MessageType = WebSocketMessage.WebSocketMessageType.Exception;
                message.Response = ex;
                Send(Serialize(message));
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
            Debug.WriteLine("Server: connection closed.");
        }

        void SendResponse(WebSocketMessage message, object response)
        {
            message.ConvertToResponse(response);
            Send(Serialize(message));
        }

        string Serialize(WebSocketMessage message)
        {
            using (System.IO.StringWriter writer = new System.IO.StringWriter())
            {
                _serializer.Serialize(writer, message);
                return writer.ToString();
            }
        }

        T Deserialize<T>(string s)
        {
            using (System.IO.StringReader stringReader = new System.IO.StringReader(s))
            using (JsonTextReader jsonReader = new JsonTextReader(stringReader))
            {
                object value = _serializer.Deserialize(jsonReader, typeof(T));
                return (T)value;
            }
        }

        void _addDelegate(IDto objectToInvoke, EventInfo ei)
        {
            delegateKey signature = new delegateKey(objectToInvoke.DtoGuid, ei.Name);
            if (_delegates.ContainsKey(signature))
                return;
            Delegate delegateToInvoke = ConvertDelegate((Action<object, EventArgs>)delegate (object o, EventArgs ea) { _notifyClient(o, ea, ei.Name); }, ei.EventHandlerType);
            Debug.WriteLine(objectToInvoke, string.Format("Server: delegate {0} added", ei.Name));
            _delegates[signature] = delegateToInvoke;
            ei.AddEventHandler(objectToInvoke, delegateToInvoke);
        }

        void _removeDelegate(IDto objectToInvoke, EventInfo ei)
        {
            delegateKey signature = new delegateKey(objectToInvoke.DtoGuid, ei.Name);
            Delegate delegateToRemove;
            if (_delegates.TryRemove(signature, out delegateToRemove))
                ei.RemoveEventHandler(objectToInvoke, delegateToRemove);
        }

        public static Delegate ConvertDelegate(Delegate originalDelegate, Type targetDelegateType)
        {
            return Delegate.CreateDelegate(
                targetDelegateType,
                originalDelegate.Target,
                originalDelegate.Method);
        }

        void _deserializeContent(ref object[] inputArray, Type[] inputTypes)
        {
            if (inputArray == null)
                return;
            for (int i = 0; i < inputArray.Length; i++)
            {
                object input = inputArray[i];
                Type type = inputTypes[i];
                if (input != null
                    && input.GetType() != type)
                {
                    if (type.IsEnum)
                        input = Enum.Parse(type, input.ToString());
                    else
                    if (type == typeof(TimeSpan))
                        input = TimeSpan.Parse((string)input, System.Globalization.CultureInfo.InvariantCulture);
                    else
                    if (type.IsValueType && type != typeof(Guid))
                        input = Convert.ChangeType(input, type);
                    else
                    if (input is JArray)
                    {
                        if (type == typeof(Guid))
                            input = new Guid(((JArray)input).First.ToString());
                        else
                        {
                            Type[] genericArgumentTypes = type.GetGenericArguments();
                            IDto[] arrayElements = Deserialize<IDto[]>((input as JContainer).ToString());
                            if (genericArgumentTypes.Length == 1)
                            {
                                Type listType = typeof(List<>);
                                input = (IList)Activator.CreateInstance(listType.MakeGenericType(genericArgumentTypes));
                                foreach (object o in arrayElements)
                                    ((IList)input).Add(o);
                            }
                        }
                    }
                    inputArray[i] = input;
                }
            }
        }

        void _notifyClient(object o, EventArgs e, string eventName)
        {
            IDto dto = o as IDto;
            if (dto == null)
                return;
            WebSocketMessage message = new WebSocketMessage()
            {
                DtoGuid = dto.DtoGuid,
                Response = e,
                MessageType = WebSocketMessage.WebSocketMessageType.EventNotification,
                MemberName = eventName,
#if DEBUG
                DtoName = dto.ToString(),
#endif
            };
            string s = Serialize(message);
            Send(s);
            Debug.WriteLine("Server: Notification {0} on {1} sent", eventName, dto);
        }

        protected override void OnError(ErrorEventArgs e)
        {
            Debug.WriteLine(e.Exception);
            base.OnError(e);
        }
    }
}
