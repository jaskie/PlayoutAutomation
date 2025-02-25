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
        public int InputSelectME { get; set; }

        [XmlAttribute]
        public int StartME { get; set; }

        [XmlAttribute]
        public int StartVideoInput { get; set; }

        [XmlIgnore]
        internal AtemController AtemController { get; set; }
    }
}
