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
        public TargetMaterial_class TargetMaterial;
        public DateTimeValue CreationDate;
        public LtcChangeTable_class LtcChangeTable;
        public Title_class Title;

        public class LtcChangeTable_class
        {
            [XmlAttribute]
            public uint tcFps;
            [XmlElement("LtcChange")]
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

        public class TargetMaterial_class
        {
            [XmlAttribute]
            public string umidRef;
        }

        public class Title_class
        {
            [XmlAttribute]
            public string usAscii;
            [XmlElement("Alias", Namespace = "urn:schemas-professionalDisc:lib")]
            public string international;
        }
    }

}
