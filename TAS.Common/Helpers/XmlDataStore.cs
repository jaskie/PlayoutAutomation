using System.IO;
using System.Xml.Serialization;

namespace TAS.Common.Helpers
{
    public static class XmlDataStore
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

        public static T Load<T>(string fileNameStem, XmlRootAttribute atrib = null)
        {
            var fullPath = $"{Path.Combine(ConfigurationFolder, fileNameStem)}.xml";
            if (!File.Exists(fullPath))
                return default;

            XmlSerializer serializer = null;
            using (var reader = new StreamReader(fullPath))
            {
                if (atrib == null)
                    serializer = new XmlSerializer(typeof(T));
                else
                    serializer = new XmlSerializer(typeof(T), atrib);
                return (T)serializer.Deserialize(reader);
            }
        }
    }
}
