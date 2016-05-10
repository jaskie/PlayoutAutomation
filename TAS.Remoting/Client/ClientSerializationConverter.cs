using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using TAS.Remoting;

namespace TAS.Remoting.Client
{
    public class ClientSerializationConverter : JsonConverter
    {
        private readonly Type iDtoType = typeof(IDto);
        private readonly ConcurrentDictionary<Guid, IDto> _dtos = new ConcurrentDictionary<Guid, IDto>();
        public override bool CanConvert(Type objectType)
        {
            return iDtoType.IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            IDto deserialized = (IDto)Activator.CreateInstance(objectType);
            serializer.Populate(reader, deserialized);
            if (deserialized != null)
            {
                IDto oldObject;
                if (_dtos.TryGetValue(((IDto)deserialized).DtoGuid, out oldObject))
                    return oldObject;
                _dtos[deserialized.DtoGuid] = deserialized;
                return deserialized;
            }
            throw new ApplicationException("ClientSerializationConverter: Dto not found");
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            JObject o = JObject.FromObject(value);
            Type t = value.GetType();
            o.AddFirst(new JProperty("$type", string.Join(", ", t.Name, t.Assembly.GetName())));
            o.WriteTo(writer);
            IDto dto = value as IDto;
            if (dto != null)
                _dtos[dto.DtoGuid] = dto;
        }

        public void Clear()
        {
            _dtos.Clear();
        }

        public bool TryGetValue(Guid guid, out IDto value)
        {
            return _dtos.TryGetValue(guid, out value);
        }

        public bool TryRemove(Guid guid, out IDto value)
        {
            Debug.WriteLine(guid, "Server: DtoSerializationConverter: Dto removed");
            return _dtos.TryRemove(guid, out value);
        }
    }
}
