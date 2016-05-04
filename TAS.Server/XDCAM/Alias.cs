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
        public List<alias_> clipTable;

        [XmlArrayItem("alias")]
        public List<alias_> editlistTable;

        public class alias_
        {
            [XmlAttribute]
            public string clipId;
            [XmlAttribute]
            public string UMID;
            [XmlAttribute]
            public string value;            
        }
    }
}
