using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Xml.Serialization;
using TAS.Common;
using TAS.Database;
using TAS.Common.Interfaces;

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
        private Principal _principal;
        private readonly object _principalLock = new object();

        [XmlIgnore]
        public override SecurityObjectType SecurityObjectTypeType { get; } = SecurityObjectType.User;

        public string AuthenticationType => _authenticationSource.ToString();

        public bool IsAuthenticated => !string.IsNullOrEmpty(Name);

        public bool IsAdmin
        {
            get { return _isAdmin; }
            set { SetField(ref _isAdmin, value); }
        }

        public AuthenticationSource AuthenticationSource
        {
            get { return _authenticationSource; }
            set { SetField(ref _authenticationSource, value); }
        }

        public string AuthenticationObject
        {
            get { return _authenticationObject; }
            set { SetField(ref _authenticationObject, value); }
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

        protected override bool SetField<T>(ref T field, T value, string propertyName = null)
        {
            lock(_principalLock)
                _principal = null;
            return base.SetField(ref field, value, propertyName);
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

        internal IPrincipal GetPrincipal()
        {
            lock (_principalLock)
            {
                if (_principal == null)
                    _principal = new Principal(this, IsAdmin ? Roles.All : new[] {Roles.Media, Roles.Preview, Roles.Playout});
                return _principal;
            }
        }
    }
}
