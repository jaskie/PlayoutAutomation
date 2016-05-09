using Newtonsoft.Json;
using System;


namespace TAS.Remoting
{
    [JsonObject(MemberSerialization.OptIn)]
    public class WebSocketMessage
    {
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
            Exception
        }


        public WebSocketMessage()
        {
            MessageGuid = Guid.NewGuid();
        }
        [JsonProperty]
        public readonly Guid MessageGuid;
        [JsonProperty]
        public Guid DtoGuid;
        [JsonProperty]
        public WebSocketMessageType MessageType;

#if DEBUG
        [JsonProperty]
        public string DtoName;
#endif

        /// <summary>
        /// Object member (method, property or event) name
        /// </summary>
        [JsonProperty]
        public string MemberName;
        [JsonProperty]
        public object[] Parameters;
        [JsonProperty(TypeNameHandling = TypeNameHandling.Auto, ItemTypeNameHandling = TypeNameHandling.Auto)]
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
