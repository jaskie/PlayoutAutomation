using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using TAS.Client.Common;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Security;

namespace TAS.Client.ViewModels
{
    public class UserManagerViewModel: ViewModelBase
    {
        private readonly IAuthenticationService _authenticationService;
        private UserViewModel _selectedUser;
        private GroupViewModel _selectedGroup;

        public UserManagerViewModel(IAuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
            Groups = new ObservableCollection<GroupViewModel>(authenticationService.Groups.Select(g => new GroupViewModel(g)));
            Users = new ObservableCollection<UserViewModel>(authenticationService.Users.Select(u => new UserViewModel(u, this)));
            CommandAddUser = new UiCommand(AddUser);
            CommandDeleteUser = new UiCommand(DeleteUser, CanDeleteUser);
            CommandAddGroup = new UiCommand(AddGroup);
            CommandDeleteGroup = new UiCommand(DeleteGroup, CanDeleteGroup);
            authenticationService.UsersOperation += AuthenticationService_UsersOperation;
            authenticationService.GroupsOperation += AuthenticationService_GroupsOperation;
        }
        
        public ObservableCollection<UserViewModel> Users { get; }

        public UserViewModel SelectedUser {
            get => _selectedUser;
            set => SetField(ref _selectedUser, value);
        }

        public ObservableCollection<GroupViewModel> Groups { get; }

        public GroupViewModel SelectedGroup {
            get => _selectedGroup;
            set => SetField(ref _selectedGroup, value);
        }

        public ICommand CommandAddUser { get; }

        public ICommand CommandDeleteUser { get; }

        public ICommand CommandAddGroup { get; }

        public ICommand CommandDeleteGroup { get; }

        protected override void OnDispose()
        {
            _authenticationService.UsersOperation -= AuthenticationService_UsersOperation;
            _authenticationService.GroupsOperation -= AuthenticationService_GroupsOperation;
            Users.ToList().ForEach(u => u.Dispose());
            Groups.ToList().ForEach(g => g.Dispose());
        }

        private void AddUser(object obj)
        {
            var newUserVm = new UserViewModel(_authenticationService.CreateUser(), this);
            Users.Add(newUserVm);
            SelectedUser = newUserVm;
        }

        private void DeleteUser(object obj)
        {
            SelectedUser.Model.Delete();
        }

        private bool CanDeleteUser(object obj)
        {
            return SelectedUser != null;
        }

        private void AddGroup(object obj)
        {
            var newVm = new GroupViewModel(_authenticationService.CreateGroup());
            Groups.Add(newVm);
            SelectedGroup = newVm;
        }

        private void DeleteGroup(object obj)
        {
            SelectedGroup.Model.Delete();
        }

        private bool CanDeleteGroup(object obj)
        {
            return SelectedGroup != null;
        }

        private void AuthenticationService_UsersOperation(object sender, CollectionOperationEventArgs<IUser> e)
        {
            OnUiThread(() =>
            {
                var vm = Users.FirstOrDefault(u => u.Model == e.Item);
                if (e.Operation == CollectionOperation.Add)
                {
                    if (vm == null)
                        Users.Add(new UserViewModel(e.Item, this));
                }
                else
                {
                    if (vm == null)
                        return;
                    Users.Remove(vm);
                    vm.Dispose();
                }
            });
        }

        private void AuthenticationService_GroupsOperation(object sender, CollectionOperationEventArgs<IGroup> e)
        {
            OnUiThread(() =>
            {
                if (e.Operation == CollectionOperation.Add)
                    Groups.Add(new GroupViewModel(e.Item));
                else
                {
                    var vm = Groups.FirstOrDefault(u => u.Model == e.Item);
                    if (vm == null)
                        return;
                    Groups.Remove(vm);
                    vm.Dispose();
                }
            });
        }

    }
}
