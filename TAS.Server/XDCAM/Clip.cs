using System.Collections.Generic;
using System.Xml.Serialization;

namespace TAS.Server.XDCAM
{
    public class Clip
    {
        [XmlAttribute] public string clipId;
        [XmlAttribute] public string umid;
        [XmlAttribute] public string fps;
        [XmlAttribute] public uint dur;
        [XmlAttribute] public bool playable;
        [XmlAttribute] public uint ch;
        [XmlAttribute] public string aspectRatio;
        [XmlAttribute] public string file;
        [XmlElement("meta")] public List<Meta> meta;
    }
}