using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace TAS.Server.XDCAM
{
    public class SerializationHelper<T>
    {
        public static T Deserialize(XmlDocument document)
        {
            XmlReader reader = new XmlNodeReader(document);
            XmlSerializer ser = new XmlSerializer(typeof(T), document.DocumentElement.FirstChild.NamespaceURI);
            return (T)ser.Deserialize(reader);
        }
    }
}
