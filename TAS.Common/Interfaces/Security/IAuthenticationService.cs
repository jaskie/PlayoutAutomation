using System;
using System.Collections.Generic;

namespace TAS.Common.Interfaces.Security
{
    public interface IAuthenticationService
    {
        IUser CreateUser();
        IGroup CreateGroup();
        bool AddUser(IUser user);
        bool RemoveUser(IUser user);
        bool AddGroup(IGroup group);
        bool RemoveGroup(IGroup group);
        event EventHandler<CollectionOperationEventArgs<IUser>> UsersOperation;
        event EventHandler<CollectionOperationEventArgs<IGroup>> GroupsOperation;
        IUser FindUser(AuthenticationSource source, string authenticationObject);
        IEnumerable<IUser> Users { get; }
        IEnumerable<IGroup> Groups { get; }
    }

    public interface IAuthenticationServicePersitency
    {
        ISecurityObject FindSecurityObject(ulong id);
    }

}