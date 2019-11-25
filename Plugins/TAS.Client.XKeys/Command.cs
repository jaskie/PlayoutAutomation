using System.Collections.Generic;
using System.Xml.Serialization;

namespace TAS.Client.XKeys
{
    public class Command
    {

        [XmlAttribute]
        public int Key { get; set; }

        [XmlAttribute]
        public string Method { get; set; }

        [XmlAttribute]
        public string Parameter { get; set; }

        [XmlAttribute]
        public CommandTargetEnum CommandTarget { get; set; }

        [XmlAttribute]
        public ActiveOnEnum ActiveOn { get; set; }

        [XmlAttribute]
        public int Required { get; set; } = -1;

    }
}
