using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace TAS.Server.Interfaces
{
    public interface IGpiConfig
    {
        [XmlAttribute]
        string Address { get; set; }
        [XmlAttribute]
        int GraphicsStartDelay { get; set; } // ms, may be negative, does not affect aspect ratio switching.
    }
}
