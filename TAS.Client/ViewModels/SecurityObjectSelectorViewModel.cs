using System.Collections.Generic;
using System.Windows.Input;
using TAS.Client.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Security;

namespace TAS.Client.ViewModels
{
    public class SecurityObjectSelectorViewModel: ViewModelBase
    {
        private ISecurityObject _selectedSecurityObject;
        
        public SecurityObjectSelectorViewModel(IAuthenticationService authenticationService)
        {
            Users = authenticationService.Users;
            Groups = authenticationService.Groups;
            CommandOk = new UiCommand(o => { }, _canOk);
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
