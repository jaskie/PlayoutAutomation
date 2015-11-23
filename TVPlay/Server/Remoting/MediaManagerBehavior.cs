using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using TAS.Server.Interfaces;
using WebSocketSharp;
using WebSocketSharp.Server;
using delegateKey = System.Tuple<System.Guid, string>;

namespace TAS.Server.Remoting
{
    public class MediaManagerBehavior : WebSocketBehavior
    {
        readonly MediaManager _mediaManager;
        public MediaManagerBehavior(MediaManager mediaManager)
        {
            _mediaManager = mediaManager;
            _dtos = new ConcurrentDictionary<Guid, IDto>();
            _delegates = new ConcurrentDictionary<delegateKey, Delegate>();
            Debug.WriteLine("Created MM behavior");
        }

        protected ConcurrentDictionary<Guid, IDto> _dtos;
        protected ConcurrentDictionary<Tuple<Guid, string>, Delegate> _delegates;

        protected override void OnMessage(MessageEventArgs e)
        {
            WebSocketMessage message = JsonConvert.DeserializeObject<WebSocketMessage>(e.Data);
            if (message.MessageType == WebSocketMessage.WebSocketMessageType.RootQuery)
            {
                message.ConvertToResponse(_mediaManager);
                Send(JsonConvert.SerializeObject(message));
                _dtos[_mediaManager.GuidDto] = _mediaManager;
            }
            else // method of particular object
            {
                if (_dtos.ContainsKey(message.DtoGuid))
                {
                    IDto objectToInvoke = _dtos[message.DtoGuid];
                    if (message.MessageType == WebSocketMessage.WebSocketMessageType.Query
                        || message.MessageType == WebSocketMessage.WebSocketMessageType.Invoke)
                    {
                        _convertParameters(ref message.Parameters);
                        Type objectToInvokeType = objectToInvoke.GetType();
                        MethodInfo methodToInvoke = objectToInvokeType.GetMethod(message.MemberName, message.Parameters.Select(p => p.GetType()).ToArray());
                        if (methodToInvoke == null)
                            methodToInvoke = objectToInvokeType.GetMethod(message.MemberName);
                        if (methodToInvoke != null)
                        {
                            _alignParameters(ref message.Parameters, methodToInvoke.GetParameters().Select(p => p.ParameterType).ToArray());
                            object response = methodToInvoke.Invoke(objectToInvoke, message.Parameters);
                            if (response != null && message.MessageType == WebSocketMessage.WebSocketMessageType.Query)
                            {
                                _registerResponse(response);
                                message.ConvertToResponse(response);
                                Send(JsonConvert.SerializeObject(message));
                            }
                        }
                    }
                    else
                    if (message.MessageType == WebSocketMessage.WebSocketMessageType.Get
                        || message.MessageType == WebSocketMessage.WebSocketMessageType.Set)
                    {
                        _convertParameters(ref message.Parameters);
                        PropertyInfo property = objectToInvoke.GetType().GetProperty(message.MemberName);
                        if (property != null)
                        {
                            if (message.MessageType == WebSocketMessage.WebSocketMessageType.Get && property.CanRead)
                            {
                                object response = property.GetValue(objectToInvoke, null);
                                if (response != null)
                                {
                                    _registerResponse(response);
                                    message.ConvertToResponse(response);
                                    Send(JsonConvert.SerializeObject(message));
                                }
                            }
                            else // Set
                            {
                                if (property.CanWrite)
                                {
                                    _alignParameters(ref message.Parameters, property.PropertyType);
                                    property.SetValue(objectToInvoke, message.Parameters[0], null);
                                }
                            }
                        }
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
                    }
                }
                else
                    throw new JsonSerializationException(string.Format("Unknown DTO: {0}", message.DtoGuid));
            }
        }

        protected override void OnClose(CloseEventArgs e)
        {
            foreach (delegateKey d in _delegates.Keys)
            {
                EventInfo ei = _dtos[d.Item1].GetType().GetEvent(d.Item2);
                Delegate delegateToRemove;
                if (_delegates.TryRemove(d, out delegateToRemove))
                    ei.RemoveEventHandler(_dtos[d.Item1], delegateToRemove);
            }
            _dtos.Clear();
        }

        void _addDelegate(IDto objectToInvoke, EventInfo ei)
        {
            delegateKey signature = new delegateKey(objectToInvoke.GuidDto, ei.Name);
            if (_delegates.ContainsKey(signature))
                return;
            Delegate delegateToInvoke = ConvertDelegate((Action<object, EventArgs>)delegate (object o, EventArgs ea) { _notifyClient(o, ea, ei.Name); }, ei.EventHandlerType);
            _delegates[signature] = delegateToInvoke;
            ei.AddEventHandler(objectToInvoke, delegateToInvoke);
        }

        void _removeDelegate(IDto objectToInvoke, EventInfo ei)
        {
            delegateKey signature = new delegateKey(objectToInvoke.GuidDto, ei.Name);
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

        static JsonSerializerSettings deserializeSettings = new JsonSerializerSettings() { MissingMemberHandling = MissingMemberHandling.Ignore };

        void _convertParameters(ref object[] input)
        {
            if (input == null)
                return;
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] is JContainer)
                    if (input[i] is JArray)
                    {
                        IDto[] inputParameters = JsonConvert.DeserializeObject<ReceivedDto[]>((input[i] as JContainer).ToString(), deserializeSettings).Cast<IDto>().ToArray();
                        for (int j = 0; j < inputParameters.Length; j++)
                            inputParameters[j] = _dtos[inputParameters[j].GuidDto];
                        input[i] = inputParameters;
                    }
                    else
                        input[i] = _dtos[JsonConvert.DeserializeObject<ReceivedDto>((input[i] as JContainer).ToString(), deserializeSettings).GuidDto];
            }
        }

        void _alignParameters(ref object[] input, params Type[] parameters)
        {
            if (input.Length != parameters.Length)
                throw new ArgumentException(string.Format("{0}:{1} {2}", this, MethodInfo.GetCurrentMethod(), "Invalid number of arguments"));
            for (int i = 0; i < input.Length; i++)
            {
                if (parameters[i].IsEnum)
                    input[i] = Enum.Parse(parameters[i], input[i].ToString());
                else
                if (parameters[i] == typeof(TimeSpan))
                    input[i] = TimeSpan.Parse((string)input[i], System.Globalization.CultureInfo.InvariantCulture);
                else
                if (parameters[i].IsValueType)
                    input[i] = Convert.ChangeType(input[i], parameters[i]);
            }
        }

        void _notifyClient(object o, EventArgs e, string eventName)
        {
            IDto dto = o as IDto;
            if (dto == null)
                return;
            WebSocketMessage message = new WebSocketMessage() { DtoGuid = dto.GuidDto, Response = e, MessageType = WebSocketMessage.WebSocketMessageType.EventNotification, MemberName = eventName };
            Send(JsonConvert.SerializeObject(message));
            Debug.WriteLine(dto, "_notifyClient executed");
        }

        protected override void OnError(ErrorEventArgs e)
        {
            Debug.WriteLine(e.Exception);
            base.OnError(e);
        }

        protected void _registerResponse(object response)
        {
            IDto responseDto = response as IDto;
            if (responseDto != null && !_dtos.ContainsKey(responseDto.GuidDto))
                _dtos[responseDto.GuidDto] = responseDto;
            if (response is System.Collections.IEnumerable)
                foreach (object o in response as System.Collections.IEnumerable)
                    if (o is IDto)
                        _dtos[(o as IDto).GuidDto] = o as IDto;
        }
    }
}
