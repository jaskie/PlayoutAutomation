using System;
using System.Collections.Generic;
using jNet.RPC;
using jNet.RPC.Server;
using TAS.Common;
using TAS.Common.Interfaces.Security;

namespace TAS.Server.Security
{
    public class AuthenticationService: ServerObjectBase, IAuthenticationService, IAuthenticationServicePersitency
    {
        private readonly AcoHive<User> _users;
        private readonly AcoHive<Group> _groups;

        private AuthenticationService()
        {
            var users = DatabaseProvider.Database.LoadSecurityObject<User>();
            var groups = DatabaseProvider.Database.LoadSecurityObject<Group>();
            foreach (var u in users)
            {
                u.AuthenticationService = this;
                u.PopulateGroups(groups);
            };
            foreach (var g in groups)
                g.AuthenticationService = this;

            _users = new AcoHive<User>(users);
            _users.AcoOperartion += Users_AcoOperation;

            _groups = new AcoHive<Group>(groups);
            _groups.AcoOperartion += Groups_AcoOperation;
        }

        public static IAuthenticationService Current { get; } = new AuthenticationService();

        [DtoMember]
        public IEnumerable<IUser> Users => _users.Items;

        [DtoMember]
        public IEnumerable<IGroup> Groups => _groups.Items;

        public IUser CreateUser() => new User(this);

        public IGroup CreateGroup() => new Group(this);

        public bool AddUser(IUser user) => _users.Add((User)user);
        
        public bool RemoveUser(IUser user) => _users.Remove((User)user);

        public bool AddGroup(IGroup group) => _groups.Add((Group)group);

        public bool RemoveGroup(IGroup group) => _groups.Remove((Group)group);

        public ISecurityObject FindSecurityObject(ulong id) => (ISecurityObject)_groups.Find(i => i.Id == id) ?? _users.Find(i => i.Id == id);

        public IUser FindUser(AuthenticationSource source, string authenticationValue)
        {
            switch (source)
            {
                case AuthenticationSource.LocalUser:
                    return _users.Find(u => u.AuthenticationSource == AuthenticationSource.LocalUser);
                case AuthenticationSource.IpAddress:
                    return _users.Find(u => u.AuthenticationSource == AuthenticationSource.IpAddress && IsIpMatch(authenticationValue, u.AuthenticationObject));
                default:
                    throw new NotImplementedException($"Authentication source {source} is not implemented.");
            }
        }

        public event EventHandler<CollectionOperationEventArgs<IUser>> UsersOperation;

        public event EventHandler<CollectionOperationEventArgs<IGroup>> GroupsOperation;

        private void Users_AcoOperation(object sender, CollectionOperationEventArgs<User> e)
        {
            UsersOperation?.Invoke(this, new CollectionOperationEventArgs<IUser>(e.Item, e.Operation));
        }

        private void Groups_AcoOperation(object sender, CollectionOperationEventArgs<Group> e)
        {
            GroupsOperation?.Invoke(this, new CollectionOperationEventArgs<IGroup>(e.Item, e.Operation));
        }

        public static bool IsIpMatch(string ip, string pattern)
        {
            if (string.IsNullOrWhiteSpace(ip) || string.IsNullOrWhiteSpace(pattern))
                return false;

            var ipParts = ip.Split('.');
            var patternParts = pattern.Split('.');

            if (ipParts.Length != 4 || patternParts.Length != 4)
                return false;

            for (int i = 0; i < 4; i++)
            {
                if (patternParts[i] == "*")
                    continue;
                if (!string.Equals(ipParts[i], patternParts[i], StringComparison.Ordinal))
                    return false;
            }
            return true;
        }
    }
}
