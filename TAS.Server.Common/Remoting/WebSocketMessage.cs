using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace TAS.Server.Remoting
{
    [DataContract]
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
            EventAdd,
            EventRemove,
            EventNotification,
            DisposeObject
        }
        [DataMember]
        public readonly Guid MessageGuid;
        [DataMember]
        public Guid DtoGuid;
        [DataMember]
        public WebSocketMessageType MessageType;
        /// <summary>
        /// Object member (method, property or event) name
        /// </summary>
        [DataMember]
        public string MemberName;
        [DataMember]
        public object[] Parameters;
        [DataMember]
        public object Response;
        public void ConvertToResponse(object response)
        {
            Response = response;
            Parameters = null;
        }

        public override string ToString()
        {
            return string.Format("WebSocketMessage: {0}:{1}:{2}", MessageType, MemberName, MessageGuid);
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
