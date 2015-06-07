using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace TAS.Server.XDCAM
{
    [XmlRoot("smil")]
    public class Smil
    {
        public const string FileExtension = ".smi";
        public Body body;

        public class Body
        {
            public Par par;
            public class Par
            {
                [XmlElement("ref")]
                public List<Ref> refList;
                public class Ref
                {
                    [XmlAttribute]
                    public string src;
                    [XmlAttribute]
                    public string clipBegin;
                    [XmlAttribute]
                    public string clipEnd;
                    [XmlAttribute]
                    public string begin;
                }
            }
        }
    }

}
