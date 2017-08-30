using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using TAS.Client.Common;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    public class UserViewmodel: EditViewmodelBase<IUser>
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
            GroupMember.CollectionChanged += _groupMember_CollectionChanged;
        }

        public string Name
        {
            get { return _name; }
            set { SetField(ref _name, value); }
        }

        public AuthenticationSource AuthenticationSource
        {
            get { return _authenticationSource; }
            set { SetField(ref _authenticationSource, value); }
        }

        public AuthenticationSource[] AuthenticationSources { get; } = Enum.GetValues(typeof(AuthenticationSource)).Cast<AuthenticationSource>().Where(a => a != AuthenticationSource.Console).ToArray();

        public string AuthenticationObject
        {
            get { return _authenticationObject; }
            set { SetField(ref _authenticationObject, value); }
        }

        public bool IsAdmin
        {
            get { return _isAdmin; }
            set { SetField(ref _isAdmin, value); }
        }

        public override void Update(object destObject = null)
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

        public ICommand CommandSave => new UICommand {ExecuteDelegate = Update, CanExecuteDelegate = o => IsModified};

        public ICommand CommandUndo => new UICommand { ExecuteDelegate = Load, CanExecuteDelegate = o => IsModified };

        protected override void OnDispose()
        {
            GroupMember.CollectionChanged -= _groupMember_CollectionChanged;
        }

        private void _groupMember_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            IsModified = true;
        }

    }
}
