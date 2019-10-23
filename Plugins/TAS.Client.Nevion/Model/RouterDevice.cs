using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace TAS.Server.Router.Model
{
    [Serializable]
    public class RouterDevice
    {
        [XmlAttribute]
        public string EngineName { get; set; }

        public string IP { get; set; }
        public int Port { get; set; }
        public Enums.Router Type { get; set; }
        public int Level { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        [XmlArray("OutputPorts")]
        [XmlArrayItem("OutputPort")]
        public List<int> OutputPorts { get; set; }

        public RouterDevice()
        {

        }
    }
}
