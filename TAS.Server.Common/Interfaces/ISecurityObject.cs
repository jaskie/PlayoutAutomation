using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TAS.Server.Common.Interfaces
{
    /// <summary>
    /// base interface for IUser and IRole
    /// </summary>
    public interface ISecurityObject
    {
        SceurityObjectType SceurityObjectTypeType { get; }
        string Name { get; set; }
    }
}
