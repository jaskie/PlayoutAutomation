using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TAS.Server.Common;
using TAS.Server.Common.Database;
using TAS.Server.Common.Interfaces;

namespace TAS.Server.Security
{
    public class AuthenticationService
    {

        private readonly AcoHive<User> _users;
        private readonly AcoHive<Role> _groups;

        public AuthenticationService(List<User> users, List<Role> groups)
        {
            users.ForEach(u => u.PopulateGroups(groups));
            _users = new AcoHive<User>(users);
            _groups = new AcoHive<Role>(groups);
        }

        public IList<User> Users => _users.Items.ToList();

        public IList<Role> Groups => _groups.Items.ToList();

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
