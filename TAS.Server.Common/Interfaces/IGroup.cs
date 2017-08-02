using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TAS.Server.Common.Interfaces
{
    public interface IGroup: ISecurityObject, IPersistent
    {
        string Name { get; set; }
    }
}
