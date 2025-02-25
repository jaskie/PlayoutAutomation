using System.Xml.Serialization;
using TAS.Common.Interfaces;

namespace TAS.Server
{
    /// <summary>
    /// Class to store the Atem device configuration
    /// </summary>
    public class AtemDevice
    {
        [XmlAttribute]
        public string EngineName { get; set; }

        [XmlAttribute]
        public string Address { get; set; }

        [XmlAttribute]
        public int MixEffectBlockIndex { get; set; } = 1;

        internal IEngine Engine;
    }
}
