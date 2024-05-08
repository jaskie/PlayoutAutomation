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
                eventRightViewmodel.ModifiedChanged += EventRightViewmodel_ModifiedChanged;
            }
            CommandAddRight = new UiCommand(CommandName(nameof(AddRight)), AddRight, CanAddRight);
            CommandDeleteRight = new UiCommand(CommandName(nameof(DeleteRight)), DeleteRight, CanDeleteRight);
            CommandOk = new UiCommand(CommandName(nameof(Save)), _ => Save(), _ => IsModified);
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

        private void Save()
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
                aclRightViewmodel.ModifiedChanged -= EventRightViewmodel_ModifiedChanged;
            }
        }

        private void AddRight(object _)
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
                SelectedRight.ModifiedChanged += EventRightViewmodel_ModifiedChanged;
                IsModified = true;
            }
        }

        private bool CanAddRight(object _) => true;

        private bool CanDeleteRight(object _) => SelectedRight != null;

        private void DeleteRight(object _)
        {
            var rightToDelete = SelectedRight;
            if (Rights.Remove(rightToDelete))
            {
                rightToDelete.Dispose();
                rightToDelete.ModifiedChanged -= EventRightViewmodel_ModifiedChanged;
                IsModified = true;
            }
        }

        private void EventRightViewmodel_ModifiedChanged(object sender, EventArgs e) => IsModified = true;

    }
}
