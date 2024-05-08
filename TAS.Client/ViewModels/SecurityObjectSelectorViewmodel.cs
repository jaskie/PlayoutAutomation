using System.Collections.Generic;
using System.Windows.Input;
using TAS.Client.Common;
using TAS.Common.Interfaces.Security;

namespace TAS.Client.ViewModels
{
    public class SecurityObjectSelectorViewmodel : ViewModelBase
    {
        private ISecurityObject _selectedSecurityObject;
        
        public SecurityObjectSelectorViewmodel(IAuthenticationService authenticationService)
        {
            Users = authenticationService.Users;
            Groups = authenticationService.Groups;
            CommandOk = new UiCommand(CommandName(nameof(CommandOk)), _ => { }, _canOk);
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

        protected override void OnDispose() { }

        private bool _canOk(object _)
        {
            return _selectedSecurityObject != null;
        }
    }
}
