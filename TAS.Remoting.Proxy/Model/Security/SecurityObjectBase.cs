using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Remoting.Client;

namespace TAS.Remoting.Model.Security
{
    /// <summary>
    /// base class for User and Group
    /// </summary>
    public abstract class SecurityObjectBase : ProxyBase, ISecurityObject
    {
        public SecurityObjectType SecurityObjectTypeType { get { return Get<SecurityObjectType>(); } set { SetLocalValue(value); } }

        public ulong Id { get; set; }

        public void Save()
        {
            Invoke();
        }

        public void Delete()
        {
            Invoke();
        }

    }
}
