using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TAS.Client.Common;
using TAS.Server.Common;
using TAS.Server.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    public class EventRightsEditViewmodel: ViewmodelBase
    {
        private readonly IEvent _ev;
        private readonly IAuthenticationService _authenticationService;
        private EventRightViewmodel _selectedRight;
        private ISecurityObject _selectedAclObject;

        public EventRightsEditViewmodel(IEvent ev, IAuthenticationService authenticationService)
        {
            _ev = ev;
            _authenticationService = authenticationService;
            AclObjects = authenticationService.Users.Cast<ISecurityObject>().Concat(authenticationService.Groups).ToArray();
            Rights = new ObservableCollection<EventRightViewmodel>(ev.Rights.Select(r => new EventRightViewmodel(r)));
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

        public ObservableCollection<EventRightViewmodel> Rights { get; }

        public EventRightViewmodel SelectedRight
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
            using (var selector = new SecurityObjectSelectorViewmodel(_authenticationService))
            {
                if (selector.ShowDialog() != true)
                    return;
                var right = _ev.AddRightFor(selector.SelectedSecurityObject);
                if (right == null)
                    return;
                var newRightVm = new EventRightViewmodel(right);
                Rights.Add(newRightVm);
                SelectedRight = newRightVm;
                IsModified = true;
            }
        }

        private bool _canAddRight(object obj)
        {
            return true;
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
