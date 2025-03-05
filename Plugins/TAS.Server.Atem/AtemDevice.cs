using System.Xml.Serialization;

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

        [XmlAttribute]
        public bool SwitchOnLoad { get; set; }

        [XmlAttribute]
        public int SwitchDelay { get; set; }

        [XmlIgnore]
        internal AtemController AtemController { get; set; }
    }
}
