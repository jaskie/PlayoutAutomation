using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using TAS.Client.Common;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    public class UserManagerViewmodel: ViewmodelBase
    {
        private readonly IAuthenticationService _authenticationService;
        private UserViewmodel _selectedUser;
        private GroupViewmodel _selectedGroup;

        public UserManagerViewmodel(IAuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
            Groups = new ObservableCollection<GroupViewmodel>(authenticationService.Groups.Select(g =>
            {
                var newVm = new GroupViewmodel(g);
                newVm.Load();
                return newVm;
            }));
            Users = new ObservableCollection<UserViewmodel>(authenticationService.Users.Select(u =>
                {
                    var newVm = new UserViewmodel(u, this);
                    newVm.Load();
                    return newVm;
                }));
            CommandAddUser = new UICommand {ExecuteDelegate = AddUser };
            CommandDeleteUser = new UICommand {ExecuteDelegate = DeleteUser, CanExecuteDelegate = CanDeleteUser};
            CommandAddGroup = new UICommand { ExecuteDelegate = AddGroup };
            CommandDeleteGroup = new UICommand { ExecuteDelegate = DeleteGroup, CanExecuteDelegate = CanDeleteGroup };
            authenticationService.UsersOperation += AuthenticationService_UsersOperation;
            authenticationService.GroupsOperation += AuthenticationService_GroupsOperation;
        }
        
        public ObservableCollection<UserViewmodel> Users { get; }

        public UserViewmodel SelectedUser { get { return _selectedUser; }  set { SetField(ref _selectedUser, value); } }

        public ObservableCollection<GroupViewmodel> Groups { get; }

        public GroupViewmodel SelectedGroup { get { return _selectedGroup; } set { SetField(ref _selectedGroup, value); } }

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
            var newUserVm = new UserViewmodel(_authenticationService.CreateUser(), this);
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
            var newVm = new GroupViewmodel(_authenticationService.CreateGroup());
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
            Application.Current.Dispatcher.BeginInvoke((Action) delegate
            {
                var vm = Users.FirstOrDefault(u => u.Model == e.Item);
                if (e.Operation == CollectionOperation.Add)
                {
                    if (vm == null)
                    {
                        vm = new UserViewmodel(e.Item, this);
                        vm.Load();
                        Users.Add(vm);
                    }
                }
                else
                {
                    if (vm != null)
                    {
                        Users.Remove(vm);
                        vm.Dispose();
                    }
                }
            });
        }

        private void AuthenticationService_GroupsOperation(object sender, CollectionOperationEventArgs<IGroup> e)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)delegate
            {
                var vm = Groups.FirstOrDefault(u => u.Model == e.Item);
                if (e.Operation == CollectionOperation.Add)
                {
                    if (vm == null)
                    {
                        vm = new GroupViewmodel(e.Item);
                        vm.Load();
                        Groups.Add(vm);
                    }
                }
                else
                {
                    if (vm != null)
                    {
                        Groups.Remove(vm);
                        vm.Dispose();
                    }
                }
            });
        }

    }
}
