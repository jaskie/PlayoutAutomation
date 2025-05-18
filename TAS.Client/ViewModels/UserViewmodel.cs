using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using TAS.Client.Common;
using TAS.Common;
using TAS.Common.Interfaces.Security;
using resources = TAS.Client.Common.Properties.Resources;


namespace TAS.Client.ViewModels
{
    public class UserViewmodel: EditViewmodelBase<IUser>, IDataErrorInfo
    {
        private string _name;
        private readonly UserManagerViewmodel _owner;
        private bool _isAdmin;
        private AuthenticationSource _authenticationSource;
        private string _authenticationObject;

        public UserViewmodel(IUser user, UserManagerViewmodel owner): base(user)
        {
            _owner = owner;
            GroupMember = new ObservableCollection<GroupViewmodel>(user.GetGroups().Select(g => owner.Groups.FirstOrDefault(vm => vm.Model == g)));
            GroupMember.CollectionChanged += GroupMember_CollectionChanged;
            CommandSave = new UiCommand(CommandName(nameof(Update)), Update, _ => IsModified);
            CommandUndo = new UiCommand(CommandName(nameof(Load)), Load, _ => IsModified);
        }

        public string Name
        {
            get => _name;
            set => SetField(ref _name, value);
        }

        public AuthenticationSource AuthenticationSource
        {
            get => _authenticationSource;
            set => SetField(ref _authenticationSource, value);
        }

        public AuthenticationSource[] AuthenticationSources { get; } = Enum.GetValues(typeof(AuthenticationSource)).Cast<AuthenticationSource>().Where(a => a != AuthenticationSource.LocalUser).ToArray();

        public string AuthenticationObject
        {
            get => _authenticationObject;
            set => SetField(ref _authenticationObject, value);
        }

        public bool IsAdmin
        {
            get => _isAdmin;
            set => SetField(ref _isAdmin, value);
        }

        protected override void Update(object destObject = null)
        {
            var actualGroups = Model.GetGroups();
            foreach (var group in AllGroups)
            {
                if (GroupMember.Contains(group) && !actualGroups.Contains(group.Model))
                    Model.GroupAdd(group.Model);
                if (!GroupMember.Contains(group) && actualGroups.Contains(group.Model))
                    Model.GroupRemove(group.Model);
            }
            base.Update(destObject);
            Model.Save();
        }

        public IList<GroupViewmodel> AllGroups => _owner.Groups;

        public ObservableCollection<GroupViewmodel> GroupMember { get; }

        public ICommand CommandSave { get; }

        public ICommand CommandUndo { get; }

        protected override void OnDispose()
        {
            GroupMember.CollectionChanged -= GroupMember_CollectionChanged;
        }

        private void GroupMember_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            IsModified = true;
        }

        public string this[string columnName]
        {
            get
            {
                switch (columnName)
                {
                    case nameof(Name):
                        if (string.IsNullOrEmpty(Name))
                            return null;
                        if (Model.FieldLengths.TryGetValue(nameof(IUser.Name), out var nameLength) && Name.Length > nameLength)
                            return resources._validate_TextTooLong;
                        break;
                    case nameof(AuthenticationObject):
                        if (string.IsNullOrEmpty(AuthenticationObject))
                            return null;
                        if (Model.FieldLengths.TryGetValue(nameof(IUser.AuthenticationObject), out var aoLength) && AuthenticationObject.Length > aoLength)
                            return resources._validate_TextTooLong;
                        break;
                }
                return null;
            }
        }

        public string Error { get; } = null;
    }
}
