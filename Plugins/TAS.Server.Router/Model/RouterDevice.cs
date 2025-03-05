using System.Xml.Serialization;
using TAS.Common.Interfaces;

namespace TAS.Server.Model
{
    public class RouterDevice
    {
        [XmlAttribute]
        public string EngineName { get; set; }
        [XmlAttribute]
        public string IpAddress { get; set; }
        [XmlAttribute]
        public int Port { get; set; }
        [XmlAttribute]
        public bool SwitchOnLoad { get; set; }
        [XmlAttribute]
        public int SwitchDelay { get; set; }
        [XmlAttribute]
        public RouterTypeEnum Type { get; set; }
        [XmlAttribute]
        public int Level { get; set; }
        [XmlAttribute]
        public string Login { get; set; }
        [XmlAttribute]
        public string Password { get; set; }
        [XmlArray("OutputPorts")]
        [XmlArrayItem("OutputPort")]
        public short[] OutputPorts { get; set; }

        internal IEngine Engine;
    }
}
