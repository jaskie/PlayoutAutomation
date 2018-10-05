using System.Collections.Generic;

namespace TAS.Common.Interfaces.Security
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
