using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace TAS.Database.Common
{
    public class ConcreteListConverter<TInterface, TImplementation> : JsonConverter where TImplementation : TInterface
    {                      
        public override bool CanConvert(Type objectType)
        {
            return true;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            serializer.TypeNameHandling = TypeNameHandling.All;            
            
            if (typeof(IEnumerable<TInterface>).IsAssignableFrom(objectType) || objectType.GetType() == typeof(IEnumerable<TInterface>))
            {                
                var list = serializer.Deserialize<List<TImplementation>>(reader);
                var interfaceList = (List<TInterface>)Activator.CreateInstance(typeof(List<TInterface>));
                foreach (var element in list)
                    interfaceList.Add(element);

                return interfaceList;                             
            }
            else
                return serializer.Deserialize<TInterface>(reader);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {            
            serializer.Serialize(writer, value);
        }
    }
}
