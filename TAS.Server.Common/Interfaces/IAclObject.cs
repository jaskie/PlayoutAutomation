using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TAS.Server.Common.Interfaces
{
    /// <summary>
    /// Object to which rights are controlled against
    /// </summary>
    public interface IAclObject
    {
        IReadOnlyCollection<IAclRight> Rights { get; }
        IAclRight AddRightFor(ISecurityObject securityObject);
        bool DeleteRight(IAclRight item);
    }
}
