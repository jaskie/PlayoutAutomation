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
    public class User: Aco, IUser, IIdentity
    {
        private readonly List<Group> _groups = new List<Group>();

        private ulong[] _groupIds;

        public User()
        {
            
        }

        [XmlIgnore]
        public override TAco AcoType { get; } = TAco.User;

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
                _groupIds = value;
            }
        }

        public void GroupAdd(Group group)
        {
            lock (((IList)_groups).SyncRoot)
            {
                _groups.Add(group);
            }
            this.DbUpdateAco();
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
                this.DbUpdateAco();
                NotifyPropertyChanged(nameof(Groups));
            }
            return isRemoved;
        }

        internal void PopulateGroups(List<Group> allGroups)
        {
            if (allGroups == null || _groupIds == null)
                return;
            lock (((IList)_groups).SyncRoot)
                Array.ForEach(_groupIds, id =>
                {
                    lock (((IList)allGroups).SyncRoot)
                    {
                        var group = allGroups.FirstOrDefault(g => g.Id == id);
                        if (group != null)
                            _groups.Add(group);
                    }
                });
            _groupIds = null;
        }

    }
}
