using System;
using System.IO;
using System.Runtime.Serialization;

namespace TAS.Remoting
{
    [Serializable]
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
            ObjectDisposed,
            Exception
        }

        public Guid MessageGuid;
        public Guid DtoGuid;
        public WebSocketMessageType MessageType;
#if DEBUG
        public string DtoName;
#endif
        /// <summary>
        /// Object member (method, property or event) name
        /// </summary>
        public string MemberName;

        /// <summary>
        /// when client invokes a method on Dto, ValueCount is count of parameters passed
        /// </summary>
        public int ValueCount;

        /// <summary>
        /// JSON-Serialized object
        /// </summary>
        public string Value;

        /// <summary>
        /// Client-side constructor
        /// </summary>


        public override string ToString()
        {
            return $"WebSocketMessage: {MessageType}:{MemberName}:{MessageGuid}";
        }

        public byte[] Serialize(IFormatter formatter)
        {
            using (var stream = new MemoryStream())
            {
                formatter.Serialize(stream, this);
                return stream.ToArray();
            }
        }

        public static WebSocketMessage Deserialize(IFormatter formatter, byte[] rawData)
        {
            using (var stream = new MemoryStream(rawData))
            {
                return (WebSocketMessage)formatter.Deserialize(stream);
            }
        }

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
