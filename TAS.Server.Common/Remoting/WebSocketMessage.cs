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
            Query,
            Response,
            Notification
        }
        public readonly Guid MessageGuid;
        public WebSocketMessageType MessageType;
        public string MethodName;
        public Array parameters;
    }
}
