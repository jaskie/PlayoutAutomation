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
            if (_dtos.ContainsKey(message.DtoGuid))
            {
                object objectToInvoke = _dtos[message.DtoGuid];
                MethodInfo methodToInvoke = objectToInvoke.GetType().GetMethod(message.MethodName);
                if (methodToInvoke != null)
                {
                    object response = methodToInvoke.Invoke(objectToInvoke, message.Parameters);
                    IDto responseDto = response as IDto;
                    if (responseDto != null && !_dtos.ContainsKey(responseDto.GuidDto))
                        _dtos[responseDto.GuidDto] = responseDto;
                    message.MakeResponse(response);
                    Send(JsonConvert.SerializeObject(message));
                }
            }
        }
        protected override void OnOpen()
        {
            Send(JsonConvert.SerializeObject(new WebSocketMessage() {DtoGuid = _mediaManager.GuidDto, MessageType = WebSocketMessage.WebSocketMessageType.InitalTransfer, Response = _mediaManager }));
            _dtos[_mediaManager.GuidDto] = _mediaManager;
        }

        protected override void OnClose(CloseEventArgs e)
        {
        }   

        protected override void OnError(ErrorEventArgs e)
        {
        }
    }
}
