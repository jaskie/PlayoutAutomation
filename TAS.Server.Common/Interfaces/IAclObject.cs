using System;
using System.Collections.Generic;
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
        List<IAclItem> Rights { get; }
        IAclItem AddRightFor(ISecurityObject securityObject);
        bool DeleteRight(IAclItem item);
    }
}
