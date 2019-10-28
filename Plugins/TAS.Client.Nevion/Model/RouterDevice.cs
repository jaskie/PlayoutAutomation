using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using TAS.Common.Interfaces;

namespace TAS.Server.Model
{
    [Serializable]
    public class RouterDevice
    {
        [XmlAttribute]
        public string EngineName { get; set; }
        [XmlAttribute]
        public string IpAddress { get; set; }
        [XmlAttribute]
        public int Port { get; set; }
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
        public List<int> OutputPorts { get; set; }

        internal IEngine Engine;
    }
}
