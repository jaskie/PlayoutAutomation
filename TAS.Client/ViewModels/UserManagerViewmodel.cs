using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TAS.Client.Common;
using TAS.Server.Common;
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
            Users = new ObservableCollection<UserViewmodel>(authenticationService.Users.Select(u =>
                {
                    var newVm = new UserViewmodel(u);
                    newVm.Load();
                    return newVm;
                }));
            CommandAddUser = new UICommand {ExecuteDelegate = AddUser };
            CommandDeleteUser = new UICommand {ExecuteDelegate = DeleteUser, CanExecuteDelegate = CanDeleteUser};
            authenticationService.UsersOperation += AuthenticationService_UsersOperation;
        }


        public string DisplayName { get; } = Common.Properties.Resources._users;

        public ObservableCollection<UserViewmodel> Users { get; }

        public UserViewmodel SelectedUser { get { return _selectedUser; }  set { SetField(ref _selectedUser, value); } }

        public ICommand CommandAddUser { get; }

        public ICommand CommandDeleteUser { get; }

        protected override void OnDispose()
        {
            _authenticationService.UsersOperation -= AuthenticationService_UsersOperation;
            Users.ToList().ForEach(u => u.Dispose());
        }

        private void AddUser(object obj)
        {
            var newUserVm = new UserViewmodel(_authenticationService.CreateUser());
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

        private void AuthenticationService_UsersOperation(object sender,
            Server.Common.CollectionOperationEventArgs<IUser> e)
        {
            Application.Current.Dispatcher.BeginInvoke((Action) delegate
            {
                if (e.Operation == CollectionOperation.Add)
                {
                    var newVm = new UserViewmodel(e.Item);
                    newVm.Load();
                    Users.Add(newVm);
                }
                else
                {
                    var oldUser = Users.FirstOrDefault(u => u.Model == e.Item);
                    if (oldUser != null)
                    {
                        Users.Remove(oldUser);
                        oldUser.Dispose();
                    }
                }
            });
        }

    }
}
