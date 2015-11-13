using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TAS.Server.Remoting
{
    [JsonObject(MemberSerialization.Fields)]
    public class WebSocketMessage
    {
        public WebSocketMessage()
        {
            MessageGuid = Guid.NewGuid();
        }
        public enum WebSocketMessageType
        {
            InitalTransfer,
            Query,
            Response,
            Notification
        }
        public readonly Guid MessageGuid;
        public Guid DtoGuid;
        public WebSocketMessageType MessageType;
        public string MethodName;
        public object[] Parameters;
        public object Response;
        public void MakeResponse(object response)
        {
            Response = response;
            MessageType = WebSocketMessageType.Response;
            Parameters = null;
        }
    }

    public class WebSocketMessageEventArgs: EventArgs
    {
        public WebSocketMessageEventArgs(WebSocketMessage message)
        {
            Message = message;
        }
        public WebSocketMessage Message { get; private set; }
    }
}
