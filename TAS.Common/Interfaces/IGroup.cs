using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TAS.Common.Interfaces.Security;

namespace TAS.Common.Interfaces
{
    public interface IGroup: ISecurityObject
    {
        string Name { get; set; }
    }
}
