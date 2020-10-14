using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using TAS.Client.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Security;


namespace TAS.Client.ViewModels
{
    public class EngineRightsEditViewModel: ModifyableViewModelBase
    {
        private readonly IEngine _engine;
        private readonly IAuthenticationService _authenticationService;
        private EngineRightViewModel _selectedRight;
        private ISecurityObject _selectedAclObject;
        private readonly List<IAclRight> _originalRights;

        public EngineRightsEditViewModel(IEngine engine, IAuthenticationService authenticationService)
        {
            _engine = engine;
            _authenticationService = authenticationService;
            AclObjects = authenticationService.Users.Cast<ISecurityObject>().Concat(authenticationService.Groups).ToArray();
            _originalRights = engine.GetRights().ToList();
            Rights = new ObservableCollection<EngineRightViewModel>(_originalRights.Select(r => new EngineRightViewModel(r)));
            foreach (var eventRightViewModel in Rights)
            {
                eventRightViewModel.ModifiedChanged += EventRightViewModelModifiedChanged;
            }
            CommandAddRight = new UiCommand(_addRight, _canAddRight);
            CommandDeleteRight = new UiCommand(_deleteRight, _canDeleteRight);
            CommandOk = new UiCommand(o => _save(), o => IsModified);
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

        public ObservableCollection<EngineRightViewModel> Rights { get; }

        public EngineRightViewModel SelectedRight
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
            foreach (var aclRightViewModel in Rights)
            {
                if (aclRightViewModel.IsModified)
                    aclRightViewModel.Save();
            }
            foreach (var right in _originalRights)
            {
                if (Rights.All(r => r.Right != right))
                    _engine.DeleteRight(right);
            }
        }

        protected override void OnDispose()
        {
            foreach (var aclRightViewModel in Rights)
            {
                aclRightViewModel.Dispose();
                aclRightViewModel.ModifiedChanged -= EventRightViewModelModifiedChanged;
            }
        }

        private void _addRight(object obj)
        {
            using (var selector = new SecurityObjectSelectorViewModel(_authenticationService))
            {
                if (WindowManager.Current.ShowDialog(selector) != true)
                    return;
                var right = _engine.AddRightFor(selector.SelectedSecurityObject);
                if (right == null)
                    return;
                var newRightVm = new EngineRightViewModel(right);
                Rights.Add(newRightVm);
                SelectedRight = newRightVm;
                SelectedRight.ModifiedChanged += EventRightViewModelModifiedChanged;
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
                rightToDelete.ModifiedChanged -= EventRightViewModelModifiedChanged;
                IsModified = true;
            }
        }

        private void EventRightViewModelModifiedChanged(object sender, EventArgs e)
        {
            IsModified = true;
        }

    }
}
