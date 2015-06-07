using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace TAS.Server.XDCAM
{

    [XmlRoot("indexFile")]
    public class Index
    {
        [XmlAttribute]
        public string proavId;
        public ClipTable clipTable;
        public EditListTable editlistTable;

        public class ClipTable
        {
            [XmlAttribute]
            public string path;
            [XmlElement("clip")]
            public List<Clip> clipTable;

        }

        public class Clip
        {
            [XmlAttribute]
            public string clipId;
            [XmlAttribute]
            public string umid;
            [XmlAttribute]
            public string fps;
            [XmlAttribute]
            public uint dur;
            [XmlAttribute]
            public bool playable;
            [XmlAttribute]
            public uint ch;
            [XmlAttribute]
            public string aspectRatio;
            [XmlAttribute]
            public string file;
            [XmlElement("meta")]
            public List<Meta> meta;
            [XmlIgnore]
            public NonRealTimeMeta ClipMeta;
        }

        public class EditListTable
        {
            [XmlAttribute]
            public string path;
            [XmlElement("editlist")]
            public List<EditList> editlistTable;
        }

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
            public List<Meta> meta;
            [XmlIgnore]
            public NonRealTimeMeta EdlMeta;
            [XmlIgnore]
            public Smil smil;
        }

        public class Meta
        {
            [XmlAttribute]
            public string file;
            [XmlAttribute]
            public string type;
        }
    }

}
