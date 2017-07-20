using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TAS.Remoting.Server;
using TAS.Server.Common;
using TAS.Server.Common.Database;
using TAS.Server.Common.Interfaces;

namespace TAS.Server.Security
{
    public class AuthenticationService: DtoBase
    {

        private readonly AcoHive<User> _users;
        private readonly AcoHive<Role> _roles;

        public AuthenticationService(List<User> users, List<Role> roles)
        {
            users.ForEach(u => u.PopulateRoles(roles));
            _users = new AcoHive<User>(users);
            _roles = new AcoHive<Role>(roles);
        }

        public IList<User> Users => _users.Items.ToList();

        public IList<Role> Roles => _roles.Items.ToList();

        public User AddUser(string userName)
        {
            return _users.Add(userName);
        }

        public bool RemoveUser(User user)
        {
            return _users.Remove(user);
        }
    }
}
