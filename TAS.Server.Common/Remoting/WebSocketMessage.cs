using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TAS.Server.Remoting
{
    [JsonObject(MemberSerialization.Fields, ItemTypeNameHandling = TypeNameHandling.None)]
    public class WebSocketMessage
    {
        public WebSocketMessage()
        {
            MessageGuid = Guid.NewGuid();
        }

        public enum WebSocketMessageType
        {
            RootQuery,
            Query,
            Invoke,
            Get,
            Set,
            Notification
        }
        public readonly Guid MessageGuid;
        public Guid DtoGuid;
        public WebSocketMessageType MessageType;
        public string MethodName;
        public object[] Parameters;
        public object Response;
        public void ConvertToResponse(object response)
        {
            Response = response;
            Parameters = null;
        }

        public override string ToString()
        {
            return string.Format("WebSocketMessage: {0}:{1}:{2}", MessageType, MethodName, MessageGuid);
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
