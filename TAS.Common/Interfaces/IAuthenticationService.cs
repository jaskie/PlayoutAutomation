using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TAS.Common.Interfaces
{
    public interface IAuthenticationService
    {
        IList<IUser> Users { get; }
        IList<IGroup> Groups { get; }
        IUser CreateUser();
        IGroup CreateGroup();
        bool AddUser(IUser user);
        bool RemoveUser(IUser user);
        bool AddGroup(IGroup group);
        bool RemoveGroup(IGroup group);
        event EventHandler<CollectionOperationEventArgs<IUser>> UsersOperation;
        event EventHandler<CollectionOperationEventArgs<IGroup>> GroupsOperation;
        IUser FindUser(AuthenticationSource source, string authenticationObject);
    }

    public interface IAuthenticationServicePersitency
    {
        ISecurityObject FindSecurityObject(ulong id);
    }

}