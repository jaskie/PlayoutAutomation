using System;
using System.Collections.Generic;
using System.Windows.Input;
using TAS.Client.Common;
using TAS.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    public class SecurityObjectSelectorViewmodel: ViewmodelBase
    {
        private ISecurityObject _selectedSecurityObject;
        
        public SecurityObjectSelectorViewmodel(IAuthenticationService authenticationService)
        {
            Users = authenticationService.Users;
            Groups = authenticationService.Groups;
            CommandOk = new UICommand { CanExecuteDelegate = _canOk };
        }

        public IEnumerable<IUser> Users { get; }
        public IEnumerable<IGroup> Groups { get; }

        public ISecurityObject SelectedSecurityObject
        {
            get => _selectedSecurityObject;
            set
            {
                if (SetField(ref _selectedSecurityObject, value))
                    InvalidateRequerySuggested();
            }
        }


        public ICommand CommandOk { get; }

        protected override void OnDispose() {}

        private bool _canOk(object obj)
        {
            return _selectedSecurityObject != null;
        }
    }
}
