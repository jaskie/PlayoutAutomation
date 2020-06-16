using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using TAS.Common.Interfaces;
using TAS.Database.Common.Interfaces;

namespace TAS.Database.Common
{
    public class PluginConverter : JsonConverter<IPlugin>
    {
        [ImportMany(typeof(IPluginTypeBinder))]        
        public static IEnumerable<IPluginTypeBinder> PluginBinders { get; }
        private static readonly string FileNameSearchPattern = "TAS.Server.*.dll";

        static PluginConverter()
        {
            var pluginPath = Path.Combine(Directory.GetCurrentDirectory(), "Plugins");

            using (var catalog = new DirectoryCatalog(pluginPath, FileNameSearchPattern))
            using (var container = new CompositionContainer(catalog))
            {
                PluginBinders = container.GetExportedValues<IPluginTypeBinder>();
            }
        }

        public override IPlugin ReadJson(JsonReader reader, Type objectType, IPlugin existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            //IPlugin plugin = (IPlugin)reader.Value;
            //if (plugin != null && plugin.IsEnabled)            
            //    return (IPlugin)reader.Value;
            //return null;            
            var jobject = JObject.Load(reader);
            var typeMeta = jobject.GetValue("$type").ToObject<string>().Split(',');

            Type type = null;
            foreach (var binder in PluginBinders)
                if ((type = binder.BindToType(typeMeta[1], typeMeta[0])) != null)
                    break;
            
            var isEnabled = jobject.GetValue("IsEnabled").ToObject<bool>();
            if (isEnabled)
            {
                var obj = Activator.CreateInstance(type);
                serializer.Populate(jobject.CreateReader(), obj);
                return (IPlugin)obj;
            }
                
                                           
            return null;
        
        }


        public override void WriteJson(JsonWriter writer, IPlugin value, JsonSerializer serializer)
        {
            writer.WriteValue(value);
        }
    }
}
