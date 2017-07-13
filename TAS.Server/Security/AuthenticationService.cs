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

        private readonly List<User> _users;
        private readonly List<Group> _groups;

        public AuthenticationService(List<User> users, List<Group> groups)
        {
            users.ForEach(u => u.PopulateGroups(groups));
            _users = users;
            _groups = groups;
        }


        public IList<User> Users
        {
            get
            {
                lock (((IList) _users).SyncRoot)
                    return _users.ToList();
            }
        }

        public IList<Group> Groups
        {
            get
            {
                lock (((IList) _groups).SyncRoot)
                    return _groups.ToList();
            }
        }

        public User AddUser(string userName)
        {
            var newUser = new User
            {
                Name = userName
            };
            lock (((IList)_users).SyncRoot)
                _users.Add(newUser);
            newUser.DbInsertAco();
            UsersOperartion?.Invoke(this, new CollectionOperationEventArgs<IUser>(newUser, CollectionOperation.Add));
            return newUser;
        }

        public bool RemoveUser(User user)
        {
            bool isRemoved;
            lock (((IList) _users).SyncRoot)
                isRemoved = _users.Remove(user);
            if (isRemoved)
            {
                user.DbDeleteAco();
                UsersOperartion?.Invoke(this, new CollectionOperationEventArgs<IUser>(user, CollectionOperation.Remove));
            }
            return isRemoved;
        }

        public event EventHandler<CollectionOperationEventArgs<IUser>> UsersOperartion;
        

    }
}
