using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TAS.Client.Common;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    public class EventRightsEditViewmodel: ViewmodelBase
    {
        private readonly IEvent _ev;
        private readonly IAuthenticationService _authenticationService;
        private EventRightViewmodel _selectedRight;
        private ISecurityObject _selectedAclObject;
        private readonly List<IAclRight> _originalRights;

        public EventRightsEditViewmodel(IEvent ev, IAuthenticationService authenticationService)
        {
            _ev = ev;
            _authenticationService = authenticationService;
            AclObjects = authenticationService.Users.Cast<ISecurityObject>().Concat(authenticationService.Groups).ToArray();
            _originalRights = ev.GetRights().ToList();
            Rights = new ObservableCollection<EventRightViewmodel>(_originalRights.Select(r => new EventRightViewmodel(r)));
            foreach (var eventRightViewmodel in Rights)
            {
                eventRightViewmodel.Modified += EventRightViewmodel_Modified;
            }
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
            foreach (var right in _originalRights)
            {
                if (!Rights.Any(r => r.Right == right))
                    _ev.DeleteRight(right);
            }
        }

        protected override void OnDispose()
        {
            foreach (var aclRightViewmodel in Rights)
            {
                aclRightViewmodel.Dispose();
                aclRightViewmodel.Modified -= EventRightViewmodel_Modified;
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
                SelectedRight.Modified += EventRightViewmodel_Modified;
                IsModified = true;
            }
        }

        private bool _canAddRight(object obj)
        {
            return true;
        }

        private bool _canDeleteRight(object obj)
        {
            return SelectedRight != null;
        }

        private void _deleteRight(object obj)
        {
            var rightToDelete = SelectedRight;
            if (Rights.Remove(rightToDelete))
            {
                rightToDelete.Dispose();
                rightToDelete.Modified -= EventRightViewmodel_Modified;
                IsModified = true;
            }
        }

        private void EventRightViewmodel_Modified(object sender, EventArgs e)
        {
            IsModified = true;
        }

    }
}
