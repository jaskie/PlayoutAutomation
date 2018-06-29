using System.Xml.Serialization;

namespace TAS.Server.XDCAM
{
    public class MediaProfile
    {
        public Material[] Contents;
    }

    public class Material
    {
        [XmlAttribute]
        public string uri;
        [XmlAttribute]
        public MaterialType type;
        [XmlAttribute]
        public string videoType;
        [XmlAttribute]
        public string audioType;
        [XmlAttribute]
        public string fps;
        [XmlAttribute]
        public int dur;
        [XmlAttribute]
        public int ch;
        [XmlAttribute]
        public string aspectRatio;
        [XmlAttribute]
        public int offset;
        [XmlAttribute]
        public string umid;
        [XmlAttribute]
        public string status;
        [XmlElement(nameof(RelevantInfo))]
        public RelevantInfo[] RelevantInfo;
    }

    public class RelevantInfo
    {
        [XmlAttribute]
        public string uri;
        [XmlAttribute]
        public RelevantInfoType type;
    }

    public enum MaterialType
    {
        [XmlEnum("MXF")]
        Mxf,
        [XmlEnum("PD-EDL")]
        Edl
    }

    public enum RelevantInfoType
    {
        [XmlEnum("XML")]
        Xml,
        [XmlEnum("KLV")]
        Klv
    }
}
