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
        private readonly List<Role> _groups = new List<Role>();

        private ulong[] _groupIds;

        [XmlIgnore]
        public override SceurityObjectType SceurityObjectTypeType { get; } = SceurityObjectType.User;

        public string AuthenticationType { get; } = "Internal";

        public bool IsAuthenticated => !string.IsNullOrEmpty(Name);

        [XmlIgnore]
        public IList<IRole> Roles
        {
            get
            {
                lock (((IList) _groups).SyncRoot)
                    return _groups.Cast<IRole>().ToList();
            }
        }

        [XmlArray(nameof(Roles)), XmlArrayItem(nameof(Role))]
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

        public void GroupAdd(Role group)
        {
            lock (((IList)_groups).SyncRoot)
            {
                _groups.Add(group);
            }
            this.DbUpdate();
            NotifyPropertyChanged(nameof(Roles));
        }

        public bool GroupRemove(Role group)
        {
            bool isRemoved;
            lock (((IList)_groups).SyncRoot)
            {
                isRemoved = _groups.Remove(group);
            }
            if (isRemoved)
            {
                this.DbUpdate();
                NotifyPropertyChanged(nameof(Roles));
            }
            return isRemoved;
        }

        internal void PopulateGroups(List<Role> allGroups)
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
