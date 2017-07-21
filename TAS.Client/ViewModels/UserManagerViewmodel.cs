using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using TAS.Client.Common;
using TAS.Server.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    public class UserManagerViewmodel: ViewmodelBase
    {
        private readonly IAuthenticationService _authenticationService;
        private UserViewmodel _selectedUser;

        public UserManagerViewmodel(IAuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
            Users = new ObservableCollection<UserViewmodel>();
            CommandAddUser = new UICommand {ExecuteDelegate = AddUser };
            CommandDeleteUser = new UICommand {ExecuteDelegate = DeleteUser, CanExecuteDelegate = CanDeleteUser};
        }

        public string DisplayName { get; } = Common.Properties.Resources._users;

        public ObservableCollection<UserViewmodel> Users { get; }

        public UserViewmodel SelectedUser { get { return _selectedUser; }  set { SetField(ref _selectedUser, value); } }

        public ICommand CommandAddUser { get; }

        public ICommand CommandDeleteUser { get; }

        protected override void OnDispose()
        {
            
        }

        private void AddUser(object obj)
        {
            var newUserVm = new UserViewmodel(_authenticationService.CreateUser());
            Users.Add(newUserVm);
            SelectedUser = newUserVm;
        }

        private void DeleteUser(object obj)
        {
            SelectedUser?.Delete();
        }

        private bool CanDeleteUser(object obj)
        {
            return SelectedUser != null;
        }


    }
}
