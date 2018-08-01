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
        public VideoType videoType;
        [XmlAttribute]
        public string audioType;
        [XmlAttribute]
        public Fps fps;
        [XmlAttribute]
        public int dur;
        [XmlAttribute]
        public int ch;
        [XmlAttribute]
        public AspectRatio aspectRatio;
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

    public enum VideoType
    {
        [XmlEnum("DV25DATA_420")]
        Dv25,
        [XmlEnum("IMX30")]
        Imx30,
        [XmlEnum("IMX40")]
        Imx40,
        [XmlEnum("IMX50")]
        Imx50,
        [XmlEnum("MPEG2HD50CBR_1280_720_422P@HL")]
        Hd720Cbr50,
        [XmlEnum("MPEG2HD35_1280_720_MP@HL")]
        Hd720Cbr35,
        [XmlEnum("MPEG2HD50CBR_1920_1080_422P@HL")]
        Hd1080Cbr50,
        [XmlEnum("MPEG2HD35_1920_1080_MP@HL")]
        Hd1080Cbr35
    }

    public enum Fps
    {
        [XmlEnum("50i")]
        Fps50I,
        [XmlEnum("59.94i")]
        Fps5994I,
        [XmlEnum("29.97p")]
        Fps2997P,
        [XmlEnum("59.94p")]
        Fps5994P,
        [XmlEnum("25p")]
        Fps25P,
        [XmlEnum("50p")]
        Fps50P,
        [XmlEnum("23.98p")]
        Fps2398P
    }

    public enum AspectRatio
    {
        [XmlEnum("4:3")]
        Narrow,
        [XmlEnum("16:9")]
        Wide
    }
    
}
