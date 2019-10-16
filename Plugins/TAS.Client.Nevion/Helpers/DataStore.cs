using System;
using System.IO;
using System.Xml.Serialization;

namespace TAS.Server.Router.Helpers
{
    public static class DataStore
    {             
        static string ConfigurationFolder = "Configuration";        

        public static void Save<T>(this T data, string fileNameStem)
        {
            if (!Directory.Exists(ConfigurationFolder))
                Directory.CreateDirectory(ConfigurationFolder);
            var fullPath = $"{Path.Combine(ConfigurationFolder, fileNameStem)}.xml";
            var serializer = new XmlSerializer(data.GetType());
            using (var writer = new StreamWriter(fullPath))
                serializer.Serialize(writer, data);
        }

        public static T Load<T>(string fileNameStem)
        {
            var fullPath = $"{Path.Combine(ConfigurationFolder, fileNameStem)}.xml";
            if (!File.Exists(fullPath))
                return default(T);

            using (var reader = new StreamReader(fullPath))
            {
                var serializer = new XmlSerializer(typeof(T));
                return (T)serializer.Deserialize(reader);
            }
        }
    }
}
