using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace TAS.Server.XDCAM
{
    [XmlRoot("aliasList")]
    public class Alias
    {
        [XmlArrayItem("alias")]
        public List<ClipAlias> clipTable;

        [XmlArrayItem("alias")]
        public List<ClipAlias> editlistTable;

        public class ClipAlias
        {
            [XmlAttribute]
            public string clipId;
            [XmlAttribute]
            public string editlistId;
            [XmlAttribute]
            public string UMID;
            [XmlAttribute]
            public string value;            
        }
    }
}
