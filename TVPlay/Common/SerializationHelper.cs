using System.IO;
using System.Xml.Serialization;

namespace TAS.Common
{
    public static class SerializationHelper
    {
        public static string Serialize<T>(T obj)
        {
            using (var outStream = new StringWriter())
            {
                var ser = new XmlSerializer(typeof(T));
                ser.Serialize(outStream, obj);
                return outStream.ToString();
            }
        }

        public static T Deserialize<T>(string serialized, string xmlAttributeOverrides = null)
        {
            using (var inStream = new StringReader(serialized))
            {
                XmlSerializer ser;
                if (string.IsNullOrEmpty(xmlAttributeOverrides))
                    ser = new XmlSerializer(typeof(T));
                else
                    ser = new XmlSerializer(typeof(T), new XmlRootAttribute(xmlAttributeOverrides));
                return (T)ser.Deserialize(inStream);
            }
        }
    }
}
