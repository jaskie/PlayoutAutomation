using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Windows.Documents;

namespace TAS.Database.Common
{
    public class ConcreteTypeConverter<TConcrete> : JsonConverter
    {        
        public override bool CanConvert(Type objectType)
        {           
            return true;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return serializer.Deserialize<TConcrete>(reader);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {         
            serializer.Serialize(writer, value);
        }
    }
}
