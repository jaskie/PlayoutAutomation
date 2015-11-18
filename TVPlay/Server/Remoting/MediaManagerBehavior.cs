using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using TAS.Server.Interfaces;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace TAS.Server.Remoting
{
    public class MediaManagerBehavior: WebSocketBehavior
    {
        readonly MediaManager _mediaManager;
        public MediaManagerBehavior(MediaManager mediaManager)
        {
            _mediaManager = mediaManager;
            _dtos = new ConcurrentDictionary<Guid, IDto>();
            Debug.WriteLine("Created MM behavior");
        }

        protected ConcurrentDictionary<Guid, IDto> _dtos;

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
                    object objectToInvoke = _dtos[message.DtoGuid];
                    if (message.MessageType == WebSocketMessage.WebSocketMessageType.Query
                        || message.MessageType == WebSocketMessage.WebSocketMessageType.Invoke)
                    {
                        MethodInfo methodToInvoke = objectToInvoke.GetType().GetMethod(message.MethodName);
                        if (methodToInvoke != null)
                        {
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
                        PropertyInfo property = objectToInvoke.GetType().GetProperty(message.MethodName);
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
                                    property.SetValue(objectToInvoke, message.Parameters[0], null);
                            }
                        }
                    }
                }
            }
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
