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
        private readonly List<Role> _roles = new List<Role>();

        private ulong[] _rolesIds;

        [XmlIgnore]
        public override SceurityObjectType SceurityObjectTypeType { get; } = SceurityObjectType.User;

        public string AuthenticationType { get; } = "Internal";

        public bool IsAuthenticated => !string.IsNullOrEmpty(Name);

        [XmlIgnore]
        public IList<IRole> Roles
        {
            get
            {
                lock (((IList) _roles).SyncRoot)
                    return _roles.Cast<IRole>().ToList();
            }
        }

        [XmlArray(nameof(Roles)), XmlArrayItem(nameof(Role))]
        public ulong[] RolesId
        {
            get
            {
                lock (((IList)_roles).SyncRoot)
                    return _roles.Select(g => g.Id).ToArray();
            }
            set
            {
                _rolesIds = value;
            }
        }

        public void RoleAdd(Role role)
        {
            lock (((IList)_roles).SyncRoot)
            {
                _roles.Add(role);
            }
            this.DbUpdate();
            NotifyPropertyChanged(nameof(Roles));
        }

        public bool RoleRemove(Role role)
        {
            bool isRemoved;
            lock (((IList)_roles).SyncRoot)
            {
                isRemoved = _roles.Remove(role);
            }
            if (isRemoved)
            {
                this.DbUpdate();
                NotifyPropertyChanged(nameof(Roles));
            }
            return isRemoved;
        }

        internal void PopulateRoles(List<Role> allRoles)
        {
            if (allRoles == null || _rolesIds == null)
                return;
            lock (((IList)_roles).SyncRoot)
                Array.ForEach(_rolesIds, id =>
                {
                    lock (((IList)allRoles).SyncRoot)
                    {
                        var group = allRoles.FirstOrDefault(g => g.Id == id);
                        if (group != null)
                            _roles.Add(group);
                    }
                });
            _rolesIds = null;
        }

    }
}
