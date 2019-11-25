using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace TAS.Server.XDCAM
{
    public class NonRealTimeMeta
    {
        [XmlAttribute]
        public DateTime lastUpdate;
        public IntegerValue Duration;
        public TargetMaterialType TargetMaterial;
        public DateTimeValue CreationDate;
        public LtcChangeTableType LtcChangeTable;
        public TitleType Title;

        public class LtcChangeTableType
        {
            [XmlAttribute]
            public uint tcFps;
            [XmlElement(nameof(LtcChange))]
            public List<LtcChange> LtcChangeTable;
        }

        public class LtcChange
        {
            [XmlAttribute]
            public uint frameCount;
            [XmlAttribute]
            public string value; //in LTC frames
            [XmlAttribute]
            public string status;
        }

        public class IntegerValue
        {
            [XmlAttribute("value")]
            public int Value;
        }
        public class DateTimeValue
        {
            [XmlAttribute("value")]
            public DateTime Value;
        }

        public class TargetMaterialType
        {
            [XmlAttribute]
            public string umidRef;
        }

        public class TitleType
        {
            [XmlAttribute]
            public string usAscii;
            [XmlElement("Alias", Namespace = "urn:schemas-professionalDisc:lib")]
            public string international;
        }
    }

}
