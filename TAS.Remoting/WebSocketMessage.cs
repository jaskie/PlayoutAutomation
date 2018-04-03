//#undef DEBUG
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace TAS.Remoting
{
    [Serializable]
    public class WebSocketMessage
    {
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

        private static readonly byte[] Version = { 0x1, 0x0,
#if DEBUG
            0x1
#else
            0x0
#endif
        };

        private readonly byte[] _rawData;
        private readonly int _valueStartIndex;

        public WebSocketMessage()
        {
            MessageGuid = Guid.NewGuid();
        }

        public WebSocketMessage(byte[] rawData)
        {
            int index = 0;
            var version = new byte[Version.Length];
            Buffer.BlockCopy(rawData, index, version, 0, version.Length);
            index += version.Length;
            MessageType = (WebSocketMessageType) rawData[index];
            index += 1;
            byte[] guidBuffer = new byte[16];
            Buffer.BlockCopy(rawData, index, guidBuffer, 0, guidBuffer.Length);
            index += guidBuffer.Length;
            MessageGuid = new Guid(guidBuffer);
            Buffer.BlockCopy(rawData, index, guidBuffer, 0, guidBuffer.Length);
            index += guidBuffer.Length;
            DtoGuid = new Guid(guidBuffer);
            int stringLength;
            if (version[2] == 0x1) // DtoName only in debug packet version
            {
                stringLength = BitConverter.ToInt32(rawData, index);
                index += sizeof(int);
#if DEBUG
                DtoName = Encoding.ASCII.GetString(rawData, index, stringLength);
#endif
                index += stringLength;
            }
            stringLength = BitConverter.ToInt32(rawData, index);
            index += sizeof(int);
            MemberName = Encoding.ASCII.GetString(rawData, index, stringLength);
            index += stringLength;
            ValueCount = BitConverter.ToInt32(rawData, index);
            _valueStartIndex = index + sizeof(int);
            _rawData = rawData;
        }

        public readonly Guid MessageGuid;
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

        public override string ToString()
        {
#if DEBUG
            return $"WebSocketMessage: {MessageType}:{MemberName}:{DtoName}";
#else
            return $"WebSocketMessage: {MessageType}:{MemberName}";
#endif
        }

        public byte[] ToByteArray(Stream value)
        {
            using (var stream = new MemoryStream())
            {
                stream.Write(Version, 0, Version.Length);

                stream.WriteByte((byte)MessageType);

                stream.Write(MessageGuid.ToByteArray(), 0, 16);

                stream.Write(DtoGuid.ToByteArray(), 0, 16);

#if DEBUG
                if (string.IsNullOrEmpty(DtoName))
                    stream.Write(BitConverter.GetBytes(0), 0, sizeof(int));
                else
                {
                    byte[] dtoName = Encoding.ASCII.GetBytes(DtoName);
                    stream.Write(BitConverter.GetBytes(dtoName.Length), 0, sizeof(int));
                    stream.Write(dtoName, 0, dtoName.Length);
                }
#endif

                // MemberName
                if (string.IsNullOrEmpty(MemberName))
                    stream.Write(BitConverter.GetBytes(0), 0, sizeof(int));
                else
                {
                    byte[] memberName = Encoding.ASCII.GetBytes(MemberName);
                    stream.Write(BitConverter.GetBytes(memberName.Length), 0, sizeof(int));
                    stream.Write(memberName, 0, memberName.Length);
                }
                stream.Write(BitConverter.GetBytes(ValueCount), 0, sizeof(int));
                if (value != null)
                {
                    value.Position = 0;
                    value.CopyTo(stream);
                }
                return stream.ToArray();
            }
        }

        public Stream GetValueStream()
        {
#if DEBUG
            var s = Encoding.UTF8.GetString(_rawData, _valueStartIndex, _rawData.Length - _valueStartIndex);
            //Debug.WriteLine(s);
#endif
            return _rawData.Length > _valueStartIndex ? new MemoryStream(_rawData, _valueStartIndex, _rawData.Length - _valueStartIndex) : null;
        }

    }

    [JsonObject(IsReference = false)]
    public class WebSocketMessageValue
    {
    
    }

    public class WebSocketMessageSingleValue : WebSocketMessageValue
    {
        [JsonProperty(TypeNameHandling = TypeNameHandling.Objects)]
        public object Value;
    }

    public class WebSocketMessageArrayValue : WebSocketMessageValue
    {
        [JsonProperty(TypeNameHandling = TypeNameHandling.Arrays, ItemTypeNameHandling = TypeNameHandling.Objects | TypeNameHandling.Arrays)]
        public object[] Value;
    }



}
