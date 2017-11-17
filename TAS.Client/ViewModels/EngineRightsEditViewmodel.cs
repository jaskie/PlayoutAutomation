using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using TAS.Client.Common;
using TAS.Common.Interfaces;
using resources = TAS.Client.Common.Properties.Resources;


namespace TAS.Client.ViewModels
{
    public class EngineRightsEditViewmodel: ViewmodelBase, ICloseable
    {
        private readonly IEngine _engine;
        private readonly IAuthenticationService _authenticationService;
        private EngineRightViewmodel _selectedRight;
        private ISecurityObject _selectedAclObject;
        private readonly List<IAclRight> _originalRights;

        public EngineRightsEditViewmodel(IEngine engine, IAuthenticationService authenticationService)
        {
            _engine = engine;
            _authenticationService = authenticationService;
            AclObjects = authenticationService.Users.Cast<ISecurityObject>().Concat(authenticationService.Groups).ToArray();
            _originalRights = engine.GetRights().ToList();
            Rights = new ObservableCollection<EngineRightViewmodel>(_originalRights.Select(r => new EngineRightViewmodel(r)));
            foreach (var eventRightViewmodel in Rights)
            {
                eventRightViewmodel.Modified += EventRightViewmodel_Modified;
            }
            CommandAddRight = new UICommand {ExecuteDelegate = _addRight, CanExecuteDelegate = _canAddRight};
            CommandDeleteRight = new UICommand { ExecuteDelegate = _deleteRight, CanExecuteDelegate = _canDeleteRight };
            CommandOk = new UICommand {ExecuteDelegate = _ok, CanExecuteDelegate = _canOk};
        }

        public ICommand CommandAddRight { get; }
               
        public ICommand CommandDeleteRight { get; }

        public ICommand CommandOk { get; }
       
        public ISecurityObject[] AclObjects { get; }

        public ISecurityObject SelectedAclObject
        {
            get { return _selectedAclObject; }
            set { SetField(ref _selectedAclObject, value, setIsModified: false); }
        }

        public ObservableCollection<EngineRightViewmodel> Rights { get; }

        public EngineRightViewmodel SelectedRight
        {
            get { return _selectedRight; }
            set { SetField(ref _selectedRight, value, setIsModified: false); }
        }

        public event EventHandler ClosedOk;

        private void _save()
        {
            foreach (var aclRightViewmodel in Rights)
            {
                if (aclRightViewmodel.IsModified)
                    aclRightViewmodel.Save();
            }
            foreach (var right in _originalRights)
            {
                if (Rights.All(r => r.Right != right))
                    _engine.DeleteRight(right);
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
                if (UiServices.ShowDialog<Views.SecurityObjectSelectorView>(selector) != true)
                    return;
                var right = _engine.AddRightFor(selector.SelectedSecurityObject);
                if (right == null)
                    return;
                var newRightVm = new EngineRightViewmodel(right);
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

        private void _ok(object obj)
        {
            _save();
            ClosedOk?.Invoke(this, EventArgs.Empty);
        }

        private bool _canOk(object obj)
        {
            return IsModified;
        }

    }
}
