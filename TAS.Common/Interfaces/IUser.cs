using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace TAS.Common.Interfaces
{
    public interface IUser: ISecurityObject, IIdentity, IPersistent
    {
        IReadOnlyCollection<IGroup> Groups { get; }
        void GroupAdd(IGroup group);
        bool GroupRemove(IGroup group);
        bool IsAdmin { get; set; }
        AuthenticationSource AuthenticationSource { get; set; }
        string AuthenticationObject { get; set; }
    }
}
