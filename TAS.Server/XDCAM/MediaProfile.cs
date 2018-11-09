using System.Xml.Serialization;

namespace TAS.Server.XDCAM
{
    public class MediaProfile
    {
        public XdcamMaterial[] Contents;
        public ProfileProperties Properties;
    }

    public class ProfileProperties
    {
        public SystemDescription System;
    }

    public class SystemDescription
    {
        [XmlAttribute]
        public string systemId;
        [XmlAttribute]
        public string systemKind;
    }

    [XmlType("Material")]
    public class XdcamMaterial
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
        [XmlEnum("PD_Proxy_Video")]
        Proxy,
        [XmlEnum("DV25DATA_411")]
        Dv411Cbr25,
        [XmlEnum("DV25DATA_420")]
        Dv420Cbr25,
        [XmlEnum("IMX30")]
        Imx30,
        [XmlEnum("IMX40")]
        Imx40,
        [XmlEnum("IMX50")]
        Imx50,

        [XmlEnum("MPEG2HD50CBR_1280_720_422P@HL")]
        Hd1280X720Cbr50,
        [XmlEnum("MPEG2HD35_1280_720_MP@HL")]
        Hd1280X720Vbr35,
        [XmlEnum("MPEG2HD25CBR_1280_720_MP@HL")]
        Hd1280X720Cbr25,

        [XmlEnum("MPEG2HD50CBR_1920_1080_422P@HL")]
        Hd1920X1080Cbr50,
        [XmlEnum("MPEG2HD35_1920_1080_MP@HL")]
        Hd1920X1080Vbr35,
        [XmlEnum("MPEG2HD25CBR_1920_540_422P@HL")]
        Hd1920X540Cbr25,


        [XmlEnum("MPEG2HD35_1440_1080_MP@HL")]
        Hd1440X1080Vbr35,
        [XmlEnum("MPEG2HD25CBR_1440_1080_MP@H-14")]
        Hd1440X1080Cbr25,
        [XmlEnum("MPEG2HD17.5_1440_1080_MP@HL")]
        Hd1440X1080Vbr175,

        [XmlEnum("MPEG2HD8.75_1440_540_MP@HL")]
        Hd1440X540Vbr875,
        [XmlEnum("MPEG2HD12.5CBR_1440_540_MP@H-14")]
        Hd1440X540Cbr125,
        [XmlEnum("MPEG2HD17.5_1440_540_MP@HL")]
        Hd1440X540Vbr175
        
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
