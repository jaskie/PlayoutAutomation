using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using TAS.Server.Interfaces;

namespace TAS.Client.Config.Model
{
    public class Gpi: ICGElementsConfig
    {
        [XmlAttribute]
        public string Address { get; set; }
        [XmlAttribute]
        public int GraphicsStartDelay { get; set; } 
    }
}
