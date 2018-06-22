using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using TAS.Client.Common;
using TAS.Common.Interfaces;


namespace TAS.Client.ViewModels
{
    public class EngineRightsEditViewmodel: ModifyableViewModelBase
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
                eventRightViewmodel.ModifiedChanged += EventRightViewmodelModifiedChanged;
            }
            CommandAddRight = new UICommand {ExecuteDelegate = _addRight, CanExecuteDelegate = _canAddRight};
            CommandDeleteRight = new UICommand { ExecuteDelegate = _deleteRight, CanExecuteDelegate = _canDeleteRight };
            CommandOk = new UICommand {ExecuteDelegate = o => _save(), CanExecuteDelegate = o => IsModified};
        }

        public ICommand CommandAddRight { get; }
               
        public ICommand CommandDeleteRight { get; }

        public ICommand CommandOk { get; }
       
        public ISecurityObject[] AclObjects { get; }

        public ISecurityObject SelectedAclObject
        {
            get => _selectedAclObject;
            set
            {
                if (_selectedAclObject == value)
                    return;
                _selectedAclObject = value;
                NotifyPropertyChanged();
            }
        }

        public ObservableCollection<EngineRightViewmodel> Rights { get; }

        public EngineRightViewmodel SelectedRight
        {
            get => _selectedRight;
            set
            {
                if (_selectedRight == value)
                    return;
                _selectedRight = value;
                NotifyPropertyChanged();
            }
        }

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
                aclRightViewmodel.ModifiedChanged -= EventRightViewmodelModifiedChanged;
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
                SelectedRight.ModifiedChanged += EventRightViewmodelModifiedChanged;
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
                rightToDelete.ModifiedChanged -= EventRightViewmodelModifiedChanged;
                IsModified = true;
            }
        }

        private void EventRightViewmodelModifiedChanged(object sender, EventArgs e)
        {
            IsModified = true;
        }

    }
}
