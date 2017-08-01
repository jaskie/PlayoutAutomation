using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Xml.Serialization;
using TAS.Server.Common;
using TAS.Server.Common.Database;
using TAS.Server.Common.Interfaces;

namespace TAS.Server.Security
{
    public class User: SecurityObjectBase, IUser, IIdentity
    {
        public User(): base(null)
        { }

        public User(IAuthenticationService authenticationService): base(authenticationService) { }

        private readonly List<Group> _groups = new List<Group>();

        private ulong[] _groupsIds;

        [XmlIgnore]
        public override SceurityObjectType SceurityObjectTypeType { get; } = SceurityObjectType.User;

        public string AuthenticationType { get; } = "Internal";

        public bool IsAuthenticated => !string.IsNullOrEmpty(Name);

        [XmlIgnore]
        public IList<IGroup> Groups
        {
            get
            {
                lock (((IList) _groups).SyncRoot)
                    return _groups.Cast<IGroup>().ToList();
            }
        }

        [XmlArray(nameof(Groups)), XmlArrayItem(nameof(Group))]
        public ulong[] GroupsId
        {
            get
            {
                lock (((IList)_groups).SyncRoot)
                    return _groups.Select(g => g.Id).ToArray();
            }
            set
            {
                _groupsIds = value;
            }
        }

        public void GroupAdd(Group group)
        {
            lock (((IList)_groups).SyncRoot)
            {
                _groups.Add(group);
            }
            this.DbUpdate();
            NotifyPropertyChanged(nameof(Groups));
        }

        public bool GroupRemove(Group group)
        {
            bool isRemoved;
            lock (((IList)_groups).SyncRoot)
            {
                isRemoved = _groups.Remove(group);
            }
            if (isRemoved)
            {
                this.DbUpdate();
                NotifyPropertyChanged(nameof(Groups));
            }
            return isRemoved;
        }

        public override void Save()
        {
            if (Id == default(ulong))
            {
                AuthenticationService.AddUser(this);
                this.DbInsert();
            }
            else
                this.DbUpdate();
        }

        public override void Delete()
        {
            AuthenticationService.RemoveUser(this);
            this.DbDelete();
        }

        internal void PopulateGroups(List<Group> allGroups)
        {
            if (allGroups == null || _groupsIds == null)
                return;
            lock (((IList)_groups).SyncRoot)
                Array.ForEach(_groupsIds, id =>
                {
                    lock (((IList)allGroups).SyncRoot)
                    {
                        var group = allGroups.FirstOrDefault(g => g.Id == id);
                        if (group != null)
                            _groups.Add(group);
                    }
                });
            _groupsIds = null;
        }

    }
}
