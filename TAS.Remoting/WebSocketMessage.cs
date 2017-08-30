using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;

namespace TAS.Remoting
{
    [JsonObject(MemberSerialization.OptIn, IsReference = false)]
    public class WebSocketMessage
    {
        [JsonConverter(typeof(StringEnumConverter))]
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
            ObjectDisposed,
            Exception
        }

        protected WebSocketMessage()
        {
        }
        [JsonProperty]
        public Guid MessageGuid;
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
        [JsonProperty(TypeNameHandling = TypeNameHandling.Auto, ItemTypeNameHandling = TypeNameHandling.Auto)]
        public object[] Parameters;
        [JsonProperty(TypeNameHandling = TypeNameHandling.Auto)]
        public object Response;

        public override string ToString()
        {
            return $"WebSocketMessage: {MessageType}:{MemberName}:{MessageGuid}";
        }

        // server-side factory
        public static WebSocketMessage Create(WebSocketMessage query, object response)
        {
            if (response is IEnumerable)
                return new WebSocketResponseArrayMessage
                {
                    MessageGuid = query.MessageGuid,
                    DtoGuid = query.DtoGuid,
                    DtoName = query.DtoName,
                    MemberName = query.MemberName,
                    MessageType = query.MessageType,
                    Response = response
                };
            return new WebSocketMessage
            {
                MessageGuid = query.MessageGuid,
                DtoGuid = query.DtoGuid,
                DtoName = query.DtoName,
                MemberName = query.MemberName,
                MessageType = query.MessageType,
                Response = response
            };
        }

        // client-side factory
        public static WebSocketMessage Create(WebSocketMessageType messageType, IDto dto, string memberName, params object[] parameters)
        {
            return new WebSocketMessage
            {
                MessageType = messageType,
                MessageGuid = Guid.NewGuid(),
                MemberName = memberName,
#if DEBUG
            DtoName = dto?.ToString(),
#endif
                DtoGuid = dto?.DtoGuid ?? Guid.Empty,

                Parameters = parameters
            };
        }
    }

    public class WebSocketResponseArrayMessage: WebSocketMessage
    {
        [JsonProperty(TypeNameHandling = TypeNameHandling.None, ItemTypeNameHandling = TypeNameHandling.Objects)]
        public new object Response;
    }

    public class WebSocketMessageEventArgs: EventArgs
    {
        public WebSocketMessageEventArgs(WebSocketMessage message)
        {
            Message = message;
        }
        public WebSocketMessage Message { get; }
    }
}
