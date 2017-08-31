using System;
using System.IO;
using System.Runtime.Serialization;
using System.Text;

namespace TAS.Remoting
{
    [Serializable]
    public class WebSocketMessage
    {
        private static readonly byte[] Version = { 0x1, 0x0,
#if DEBUG
    0x1
#else
    0x0
#endif
        };
        public enum WebSocketMessageType: byte
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

        public byte[] Serialize()
        {
            using (var stream = new MemoryStream())
            {
                stream.Write(Version, 0, Version.Length);
                stream.WriteByte((byte)MessageType);
                stream.Write(MessageGuid.ToByteArray(), 0, 16);
                stream.Write(DtoGuid.ToByteArray(), 0, 16);
                if (Version[2] == 0x1) // debug packet version
                {
                    if (string.IsNullOrEmpty(DtoName))
                        stream.Write(BitConverter.GetBytes(0), 0, sizeof(int));
                    else
                    {
                        byte[] dtoName = Encoding.ASCII.GetBytes(DtoName);
                        stream.Write(BitConverter.GetBytes(dtoName.Length), 0, sizeof(int));
                        stream.Write(dtoName, 0, dtoName.Length);
                    }
                }
                if (string.IsNullOrEmpty(MemberName))
                    stream.Write(BitConverter.GetBytes(0), 0, sizeof(int));
                else
                {
                    byte[] memberName = Encoding.ASCII.GetBytes(MemberName);
                    stream.Write(BitConverter.GetBytes(memberName.Length), 0, sizeof(int));
                    stream.Write(memberName, 0, memberName.Length);
                }

                return stream.ToArray();
            }
        }
        public Stream GetValueStream()
        {
            throw new NotImplementedException();
        }


        public static WebSocketMessage Deserialize(byte[] rawData)
        {
            using (var stream = new MemoryStream(rawData))
            {
                throw new NotImplementedException();
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
