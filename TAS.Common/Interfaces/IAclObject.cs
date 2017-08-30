using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TAS.Common.Interfaces
{
    /// <summary>
    /// Object to which rights are controlled against
    /// </summary>
    public interface IAclObject
    {
        IEnumerable<IAclRight> GetRights();

        IAclRight AddRightFor(ISecurityObject securityObject);

        bool DeleteRight(IAclRight item);
    }
}
