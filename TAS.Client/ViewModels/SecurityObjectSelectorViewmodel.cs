using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TAS.Client.Common;
using TAS.Common.Interfaces;
using resources = TAS.Client.Common.Properties.Resources;

namespace TAS.Client.ViewModels
{
    public class SecurityObjectSelectorViewmodel: OkCancelViewmodelBase
    {
        private ISecurityObject _selectedSecurityObject;
        
        public SecurityObjectSelectorViewmodel(IAuthenticationService authenticationService): base(typeof(Views.SecurityObjectSelectorView), resources._window_SecurityObjectSelectorWindowTitle)
        {
            Users = authenticationService.Users;
            Groups = authenticationService.Groups;
        }

        public IList<IUser> Users { get; }
        public IList<IGroup> Groups { get; }

        public ISecurityObject SelectedSecurityObject
        {
            get { return _selectedSecurityObject; }
            set
            {
                if (SetField(ref _selectedSecurityObject, value))
                    InvalidateRequerySuggested();
            }
        }

        protected override bool CanOk(object parameter)
        {
            return _selectedSecurityObject != null;
        }

        protected override void OnDispose()
        {
            
        }
    }
}
