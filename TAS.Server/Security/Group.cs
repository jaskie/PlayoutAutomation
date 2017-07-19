using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using TAS.Server.Common;
using TAS.Server.Common.Interfaces;

namespace TAS.Server.Security
{
    public class Group: Aco, IGroup
    {
        [XmlIgnore]
        public override TAco AcoType { get; } = TAco.Group;
    }
}
