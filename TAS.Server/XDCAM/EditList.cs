using System.Xml.Serialization;

namespace TAS.Server.XDCAM
{
    public class EditList
    {
        [XmlAttribute]
        public string editlistId;
        [XmlAttribute]
        public string umid;
        [XmlAttribute]
        public string fps;
        [XmlAttribute]
        public uint dur;
        public uint ch;
        [XmlAttribute]
        public string aspectRatio;
        [XmlAttribute]
        public string file;
        [XmlElement("meta")]
        public Meta[] meta;
    }
}