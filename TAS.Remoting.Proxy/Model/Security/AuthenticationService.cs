using System;
using System.Collections.Generic;
using jNet.RPC;
using jNet.RPC.Client;
using TAS.Common;
using TAS.Common.Interfaces.Security;

namespace TAS.Remoting.Model.Security
{
    public class AuthenticationService: ProxyObjectBase, IAuthenticationService
    {
#pragma warning disable CS0649
        [DtoMember(nameof(IAuthenticationService.Users))]
        private List<User> _users;

        [DtoMember(nameof(IAuthenticationService.Groups))]
        private List<Group> _groups;

        private event EventHandler<CollectionOperationEventArgs<IUser>> _usersOperation;

        private event EventHandler<CollectionOperationEventArgs<IGroup>> _groupsOperation;
#pragma warning restore

        public IEnumerable<IUser> Users => _users;

        public IEnumerable<IGroup> Groups => _groups;

        public IUser CreateUser() => Query<User>();
        
        public IGroup CreateGroup() => Query<Group>();

        public bool AddUser(IUser user) => Query<bool>(parameters: new object[] {user});
        
        public bool RemoveUser(IUser user) => Query<bool>(parameters: new object[] { user });

        public bool AddGroup(IGroup group) => Query<bool>(parameters: new object[] { group });

        public bool RemoveGroup(IGroup group) => Query<bool>(parameters: new object[] { group });

        public event EventHandler<CollectionOperationEventArgs<IUser>> UsersOperation
        {
            add
            {
                EventAdd(_usersOperation);
                _usersOperation += value;
            }
            remove
            {
                _usersOperation -= value;
                EventRemove(_usersOperation);
            }
        }

        public event EventHandler<CollectionOperationEventArgs<IGroup>> GroupsOperation
        {
            add
            {
                EventAdd(_groupsOperation);
                _groupsOperation += value;
            }
            remove
            {
                _groupsOperation -= value;
                EventRemove(_groupsOperation);
            }
        }

        public IUser FindUser(AuthenticationSource source, string authenticationObject)
        {
            throw new NotImplementedException();
        }

        protected override void OnEventNotification(SocketMessage message)
        {
            switch (message.MemberName)
            {

                case nameof(UsersOperation):
                    _usersOperation?.Invoke(this, DeserializeEventArgs<CollectionOperationEventArgs<IUser>>(message));
                    break;
                case nameof(GroupsOperation):
                    _groupsOperation?.Invoke(this, DeserializeEventArgs<CollectionOperationEventArgs<IGroup>>(message));
                    break;
            }
        }
    }
}
