using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace TAS.Client.Router.Model
{
    public class Enums
    {
        [Serializable]
        public enum Router
        {
            [XmlEnum(Name = "Nevion")]
            Nevion,
            [XmlEnum(Name = "Blackmagic")]
            Blackmagic
        }

        public enum ListType
        {
            Input,
            Output
        }
    }
}
