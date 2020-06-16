using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace TAS.Database.Common
{
    public class TestConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return true;
        }
        
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jobject = JObject.Load(reader);
            return serializer.Deserialize(reader);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }
}
