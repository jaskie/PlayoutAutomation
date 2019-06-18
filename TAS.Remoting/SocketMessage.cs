using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace TAS.Remoting
{
    public class SocketMessage
    {
        public enum SocketMessageType: byte
        {
            RootQuery,
            Query,
            Invoke,
            Get,
            Set,
            EventAdd,
            EventRemove,
            EventNotification,
            Exception,
            ProxyFinalized
        }

        private static readonly byte[] Version = { 0x1, 0x0,
#if DEBUG
            0x1
#else
            0x0
#endif
        };

        private readonly byte[] _rawData;
        private readonly int _valueStartIndex;

        public SocketMessage(object value)
        {
            MessageGuid = Guid.NewGuid();
            Value = value;
        }

        public SocketMessage(SocketMessage originalMessage, object value)
        {
            MessageGuid = originalMessage.MessageGuid;
            MessageType = originalMessage.MessageType;
            DtoGuid = originalMessage.DtoGuid;
            MemberName = originalMessage.MemberName;
            Value = value;
        }

        public SocketMessage(byte[] rawData)
        {
            var index = 0;
            var version = new byte[Version.Length];
            Buffer.BlockCopy(rawData, index, version, 0, version.Length);
            index += version.Length;
            MessageType = (SocketMessageType) rawData[index];
            index += 1;
            byte[] guidBuffer = new byte[16];
            Buffer.BlockCopy(rawData, index, guidBuffer, 0, guidBuffer.Length);
            index += guidBuffer.Length;
            MessageGuid = new Guid(guidBuffer);
            Buffer.BlockCopy(rawData, index, guidBuffer, 0, guidBuffer.Length);
            index += guidBuffer.Length;
            DtoGuid = new Guid(guidBuffer);
            var stringLength = BitConverter.ToInt32(rawData, index);
            index += sizeof(int);
            MemberName = Encoding.ASCII.GetString(rawData, index, stringLength);
            index += stringLength;
            ParametersCount = BitConverter.ToInt32(rawData, index);
            _valueStartIndex = index + sizeof(int);
            _rawData = rawData;
        }

        public object Value { get; }
        public readonly Guid MessageGuid;
        public Guid DtoGuid;
        public SocketMessageType MessageType;
        /// <summary>
        /// Object member (method, property or event) name
        /// </summary>
        public string MemberName;

        /// <summary>
        /// count of parameters passed from client to server, 
        /// </summary>
        public int ParametersCount;

        public override string ToString()
        {
            return $"WebSocketMessage: {MessageType}:{MemberName}";
        }

        public byte[] ToByteArray(Stream value)
        {
            using (var stream = new MemoryStream())
            {
                stream.Write(new byte[]{0, 0, 0, 0}, 0, sizeof(int)); // content length placeholder

                stream.Write(Version, 0, Version.Length);

                stream.WriteByte((byte)MessageType);

                stream.Write(MessageGuid.ToByteArray(), 0, 16);

                stream.Write(DtoGuid.ToByteArray(), 0, 16);

                // MemberName
                if (string.IsNullOrEmpty(MemberName))
                    stream.Write(BitConverter.GetBytes(0), 0, sizeof(int));
                else
                {
                    byte[] memberName = Encoding.ASCII.GetBytes(MemberName);
                    stream.Write(BitConverter.GetBytes(memberName.Length), 0, sizeof(int));
                    stream.Write(memberName, 0, memberName.Length);
                }
                stream.Write(BitConverter.GetBytes(ParametersCount), 0, sizeof(int));
                if (value != null)
                {
                    value.Position = 0;
                    value.CopyTo(stream);
                }
                var contentLength = BitConverter.GetBytes((int)stream.Length - sizeof(int));
                stream.Position = 0;
                stream.Write(contentLength, 0, sizeof(int));
                return stream.ToArray();
            }
        }

        public Stream ValueStream => _rawData.Length > _valueStartIndex ? new MemoryStream(_rawData, _valueStartIndex, _rawData.Length - _valueStartIndex) : null;
        
        public string ValueString => Encoding.UTF8.GetString(_rawData, _valueStartIndex, _rawData.Length - _valueStartIndex);
    }

    [JsonObject(IsReference = false)]
    public class SocketMessageArrayValue 
    {
        [JsonProperty(TypeNameHandling = TypeNameHandling.Arrays, ItemTypeNameHandling = TypeNameHandling.Objects | TypeNameHandling.Arrays)]
        public object[] Value;
    }



}
