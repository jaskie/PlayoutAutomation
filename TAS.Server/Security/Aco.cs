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
    /// Access Control Object - base class for User, Group and Role
    /// </summary>
    public abstract class Aco: DtoBase, IAco, IPersistent
    {
        private string _name;
        public abstract TAco AcoType { get; }

        [XmlIgnore]
        public ulong Id { get; set; }

        public string Name
        {
            get { return _name; }
            set
            {
                if (SetField(ref _name, value))
                    this.DbUpdateAco();
            }
        }

    }
}
