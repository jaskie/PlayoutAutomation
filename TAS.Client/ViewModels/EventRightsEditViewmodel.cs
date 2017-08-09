using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TAS.Client.Common;
using TAS.Server.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    public class EventRightsEditViewmodel: ViewmodelBase
    {
        private readonly IEvent _ev;
        private readonly IAuthenticationService _authenticationService;
        private AclRightViewmodel _selectedRight;
        private ISecurityObject _selectedAclObject;

        public EventRightsEditViewmodel(IEvent ev, IAuthenticationService authenticationService)
        {
            _ev = ev;
            _authenticationService = authenticationService;
            AclObjects = authenticationService.Users.Cast<ISecurityObject>().Concat(authenticationService.Groups).ToArray();
            Rights = new ObservableCollection<AclRightViewmodel>(ev.Rights.Select(r => new AclRightViewmodel(r)));
            CommandAddRight = new UICommand {ExecuteDelegate = _addRight, CanExecuteDelegate = _canAddRight};
            CommandDeleteRight = new UICommand { ExecuteDelegate = _deleteRight, CanExecuteDelegate = _canDeleteRight };
        }


        public UICommand CommandAddRight { get; }
        public UICommand CommandDeleteRight { get; }
        

        public ISecurityObject[] AclObjects { get; }

        public ISecurityObject SelectedAclObject
        {
            get { return _selectedAclObject; }
            set { SetField(ref _selectedAclObject, value, setIsModified: false); }
        }

        public ObservableCollection<AclRightViewmodel> Rights { get; }

        public AclRightViewmodel SelectedRight
        {
            get { return _selectedRight; }
            set { SetField(ref _selectedRight, value, setIsModified: false); }
        }

        public void Save()
        {
            foreach (var aclRightViewmodel in Rights)
            {
                if (aclRightViewmodel.IsModified)
                    aclRightViewmodel.Save();
            }
        }


        protected override void OnDispose()
        {
            foreach (var aclRightViewmodel in Rights)
            {
                aclRightViewmodel.Dispose();
            }
        }

        private void _addRight(object obj)
        {
            var right = _ev.AddRightFor(_selectedAclObject);
            if (right == null)
                return;
            Rights.Add(new AclRightViewmodel(right));
            IsModified = true;
        }

        private bool _canAddRight(object obj)
        {
            return _selectedAclObject != null;
        }

        private bool _canDeleteRight(object obj)
        {
            return _selectedRight != null;
        }

        private void _deleteRight(object obj)
        {
            var rightToDelete = _selectedRight;
            if (_ev.DeleteRight(rightToDelete.Right)
                && Rights.Remove(rightToDelete))
            { 
                rightToDelete.Dispose();
            }
        }


    }
}
