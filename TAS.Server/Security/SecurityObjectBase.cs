using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using TAS.Remoting.Server;
using TAS.Server.Common;
using TAS.Server.Common.Database;
using TAS.Server.Common.Interfaces;

namespace TAS.Server.Security
{
    /// <summary>
    /// base class for User and Role
    /// </summary>
    public abstract class SecurityObjectBase: DtoBase, ISecurityObject, IPersistent
    {
        private string _name;
        public abstract SceurityObjectType SceurityObjectTypeType { get; }

        [XmlIgnore]
        public ulong Id { get; set; }

        public string Name
        {
            get { return _name; }
            set
            {
                if (SetField(ref _name, value))
                    this.DbUpdate();
            }
        }

    }
}
