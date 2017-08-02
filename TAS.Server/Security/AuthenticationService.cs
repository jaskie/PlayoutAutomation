using System;
using System.Collections.Generic;
using TAS.Remoting.Server;
using TAS.Server.Common;
using TAS.Server.Common.Interfaces;

namespace TAS.Server.Security
{
    public class AuthenticationService: DtoBase, IAuthenticationService
    {
        private readonly AcoHive<User> _users;
        private readonly AcoHive<Group> _groups;

        public AuthenticationService(List<User> users, List<Group> groups)
        {
            users.ForEach(u =>
            {
                u.AuthenticationService = this;
                u.PopulateGroups(groups);
            });
            groups.ForEach(g => g.AuthenticationService = this);

            _users = new AcoHive<User>(users);
            _users.AcoOperartion += Users_AcoOperation;

            _groups = new AcoHive<Group>(groups);
            _groups.AcoOperartion += Groups_AcoOperation;
        }

        public IEnumerable<IUser> Users => _users.Items;

        public IEnumerable<IGroup> Groups => _groups.Items;

        public IUser CreateUser() => new User(this);

        public IGroup CreateGroup() => new Group(this);

        public bool AddUser(IUser user) => _users.Add((User)user);
        
        public bool RemoveUser(IUser user) => _users.Remove((User)user);

        public bool AddGroup(IGroup group) => _groups.Add((Group)group);

        public bool RemoveGroup(IGroup group) => _groups.Remove((Group)group);


        public event EventHandler<CollectionOperationEventArgs<IUser>> UsersOperation;

        public event EventHandler<CollectionOperationEventArgs<IGroup>> GroupsOperation;

        protected override void DoDispose()
        {
            _users.AcoOperartion -= Users_AcoOperation;
            _groups.AcoOperartion -= Groups_AcoOperation;
            base.DoDispose();
        }

        private void Users_AcoOperation(object sender, CollectionOperationEventArgs<User> e)
        {
            UsersOperation?.Invoke(this, new CollectionOperationEventArgs<IUser>(e.Item, e.Operation));
        }

        private void Groups_AcoOperation(object sender, CollectionOperationEventArgs<Group> e)
        {
            GroupsOperation?.Invoke(this, new CollectionOperationEventArgs<IGroup>(e.Item, e.Operation));
        }
    }
}
