using jNet.RPC;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Serialization;
using TAS.Common;
using TAS.Database.Common;
using TAS.Common.Interfaces.Security;

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
        private AuthenticationSource _authenticationSource;
        private string _authenticationObject;

        [DtoMember, XmlIgnore]
        public override SecurityObjectType SecurityObjectTypeType { get; } = SecurityObjectType.User;

        [DtoMember]
        public string AuthenticationType => _authenticationSource.ToString();

        [DtoMember]
        public bool IsAuthenticated => !string.IsNullOrEmpty(Name);

        [DtoMember, Hibernate]
        public bool IsAdmin
        {
            get { return _isAdmin; }
            set { SetField(ref _isAdmin, value); }
        }

        [DtoMember, Hibernate]
        public AuthenticationSource AuthenticationSource
        {
            get { return _authenticationSource; }
            set { SetField(ref _authenticationSource, value); }
        }

        [DtoMember, Hibernate]
        public string AuthenticationObject
        {
            get { return _authenticationObject; }
            set { SetField(ref _authenticationObject, value); }
        }

        public ReadOnlyCollection<IGroup> GetGroups()
        {
            lock (_groups.SyncRoot())
                return _groups.AsReadOnly();
        }

        [XmlArray("Groups"), XmlArrayItem(nameof(Group))]
        [Hibernate("Groups")]
        public ulong[] GroupsId
        {
            get
            {
                lock (_groups.SyncRoot())
                    return _groups.Select(g => g.Id).ToArray();
            }
            set
            {
                _groupsIds = value;
            }
        }

        public void GroupAdd(IGroup group)
        {
            lock (_groups.SyncRoot())
            {
                _groups.Add(group);
            }
        }

        public bool GroupRemove(IGroup group)
        {
            bool isRemoved;
            lock (_groups.SyncRoot())
            {
                isRemoved = _groups.Remove(group);
            }
            return isRemoved;
        }

        public override void Save()
        {
            if (Id == default(ulong))
            {
                AuthenticationService.AddUser(this);
                DatabaseProvider.Database.InsertSecurityObject(this);
            }
            else
                DatabaseProvider.Database.UpdateSecurityObject(this);
        }

        public override void Delete()
        {
            AuthenticationService.RemoveUser(this);
            DatabaseProvider.Database.DeleteSecurityObject(this);
        }

        internal void PopulateGroups(List<Group> allGroups)
        {
            if (allGroups == null || _groupsIds == null)
                return;
            lock (_groups.SyncRoot())
                Array.ForEach(_groupsIds, id =>
                {
                    lock (allGroups.SyncRoot())
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
