using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TAS.Server.Common.Interfaces
{
    public interface IAuthenticationService
    {
        IEnumerable<IUser> Users { get; }
        IEnumerable<IGroup> Groups { get; }
        IUser CreateUser();
        IGroup CreateGroup();
        bool AddUser(IUser user);
        bool RemoveUser(IUser user);
        bool AddGroup(IGroup group);
        bool RemoveGroup(IGroup group);
        event EventHandler<CollectionOperationEventArgs<IUser>> UsersOperation;
        event EventHandler<CollectionOperationEventArgs<IGroup>> GroupsOperation;

    }
}