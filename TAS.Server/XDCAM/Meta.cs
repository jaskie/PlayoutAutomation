using System.Xml.Serialization;

namespace TAS.Server.XDCAM
{
    public class Meta
    {
        [XmlAttribute]
        public string file;
        [XmlAttribute]
        public string type;
    }
}