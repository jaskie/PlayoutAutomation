using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using TAS.Client.Config;
using TAS.Database.Common.Interfaces;

namespace TAS.Database.Common
{
    public class ConcreteListConverter<TInterface, TImplementation> : JsonConverter where TImplementation : TInterface
    {
        private static IEnumerable<IPluginTypeBinder> _pluginBinders = ConfigurationPluginManager.Current.PluginTypeBinders;
        
        private TInterface Create(Type objectType, JObject jObject)
        {
            Type type = null;
            var stringType = jObject["Type"].Value<string>().Split(',');
            
            foreach (var binder in _pluginBinders)
                type = binder.BindToType(stringType[1], stringType[0]);

            return (TInterface)Activator.CreateInstance(type);
        }

        public override bool CanConvert(Type objectType)
        {
            return true;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            serializer.TypeNameHandling = TypeNameHandling.All;            
            
            if (typeof(IEnumerable<TInterface>).IsAssignableFrom(objectType) || objectType.GetType() == typeof(IEnumerable<TInterface>))
            {
                var jObject = JObject.Load(reader);
                var target = Create(objectType, jObject);                
                serializer.Populate(jObject.CreateReader(), target);
                return target;

                //var list = serializer.Deserialize<List<TInterface>>(reader);
                //var interfaceList = (List<TInterface>)Activator.CreateInstance(typeof(List<TInterface>));
                //foreach (var element in list)
                //    interfaceList.Add(element);

                //return interfaceList;
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
