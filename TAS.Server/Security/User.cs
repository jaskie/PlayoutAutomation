using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using TAS.Server.Common;
using TAS.Server.Common.Database;
using TAS.Server.Common.Interfaces;

namespace TAS.Server.Security
{
    public class User: SecurityObjectBase, IUser
    {
        public User(): base(null)
        { }

        public User(IAuthenticationService authenticationService): base(authenticationService) { }

        private readonly List<IGroup> _groups = new List<IGroup>();

        private ulong[] _groupsIds;
        private bool _isAdmin;

        [XmlIgnore]
        public override SceurityObjectType SceurityObjectTypeType { get; } = SceurityObjectType.User;

        public string AuthenticationType { get; } = "Internal";

        public bool IsAuthenticated => !string.IsNullOrEmpty(Name);

        public bool IsAdmin
        {
            get { return _isAdmin; }
            set { SetField(ref _isAdmin, value); }
        }

        [XmlIgnore]
        public IReadOnlyCollection<IGroup> Groups
        {
            get
            {
                lock (((IList) _groups).SyncRoot)
                    return _groups.AsReadOnly();
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

        public void GroupAdd(IGroup group)
        {
            lock (((IList)_groups).SyncRoot)
            {
                _groups.Add(group);
            }
            NotifyPropertyChanged(nameof(Groups));
        }

        public bool GroupRemove(IGroup group)
        {
            bool isRemoved;
            lock (((IList)_groups).SyncRoot)
            {
                isRemoved = _groups.Remove(group);
            }
            if (isRemoved)
                NotifyPropertyChanged(nameof(Groups));
            return isRemoved;
        }

        public override void Save()
        {
            if (Id == default(ulong))
            {
                AuthenticationService.AddUser(this);
                this.DbInsertSecurityObject();
            }
            else
                this.DbUpdateSecurityObject();
        }

        public override void Delete()
        {
            AuthenticationService.RemoveUser(this);
            this.DbDeleteSecurityObject();
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
