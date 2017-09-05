using System.Security.Principal;
using TAS.Common.Interfaces;

namespace TAS.Server.Security
{
    public class Principal: IPrincipal
    {
        private readonly string[] _roles;
        public Principal(IUser user)
        {
            Identity = user;
        }

        public bool IsInRole(string role)
        {
            return false;
        }

        public IIdentity Identity { get; }
    }
}
