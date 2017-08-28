using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using TAS.Common.Interfaces;

namespace TAS.Server.Security
{
    public class Principal: IPrincipal
    {
        private readonly string[] _roles;
        public Principal(IUser user, params string[] roles)
        {
            Identity = user;
            _roles = roles;
        }

        public bool IsInRole(string role)
        {
            return _roles.Contains(role);
        }

        public IIdentity Identity { get; }
    }
}
