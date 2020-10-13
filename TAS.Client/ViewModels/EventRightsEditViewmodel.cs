﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using TAS.Client.Common;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Security;


namespace TAS.Client.ViewModels
{
    public class EventRightsEditViewmodel: ModifyableViewModelBase
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
            Rights = new ObservableCollection<EventRightViewmodel>();
            Load();
            CommandAddRight = new UiCommand(_addRight, _canAddRight);
            CommandDeleteRight = new UiCommand(_deleteRight, _canDeleteRight);
        }

        public UiCommand CommandAddRight { get; }

        public UiCommand CommandDeleteRight { get; }
        
        public ISecurityObject[] AclObjects { get; }

        public ISecurityObject SelectedAclObject
        {
            get => _selectedAclObject;
            set
            {
                if (_selectedAclObject ==  value)
                    return;
                _selectedAclObject = value;
                NotifyPropertyChanged();
            }
        }

        public ObservableCollection<EventRightViewmodel> Rights { get; }

        public EventRightViewmodel SelectedRight
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

       

        public void UndoEdit()
        {
            foreach (var rightViewmodel in Rights)
            {
                rightViewmodel.ModifiedChanged -= EventRightViewmodelModifiedChanged;
                rightViewmodel.Dispose();
            }
            Rights.Clear();
            Load();
            NotifyPropertyChanged(nameof(Rights));
            IsModified = false;
        }

        public void Save()
        {
            if (!IsModified)
                return;
            foreach (var aclRightViewmodel in Rights)
            {
                if (aclRightViewmodel.IsModified)
                    aclRightViewmodel.Save();
            }
            foreach (var right in _originalRights)
            {
                if (Rights.All(r => r.Right != right))
                    _ev.DeleteRight(right);
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
                if  (WindowManager.Current.ShowDialog(selector) != true)
                    return;
                var right = _ev.AddRightFor(selector.SelectedSecurityObject);
                if (right == null)
                    return;
                var newRightVm = new EventRightViewmodel(right);
                Rights.Add(newRightVm);
                SelectedRight = newRightVm;
                SelectedRight.ModifiedChanged += EventRightViewmodelModifiedChanged;
                IsModified = true;
            }
        }

        private bool _canAddRight(object obj)
        {
            return _ev.Engine.HaveRight(EngineRight.RundownRightsAdmin);
        }

        private bool _canDeleteRight(object obj)
        {
            return SelectedRight != null && _ev.Engine.HaveRight(EngineRight.RundownRightsAdmin);
        }

        private void _deleteRight(object obj)
        {
            var rightToDelete = SelectedRight;
            if (!Rights.Remove(rightToDelete))
                return;
            rightToDelete.Dispose();
            rightToDelete.ModifiedChanged -= EventRightViewmodelModifiedChanged;
            IsModified = true;
        }

        private void EventRightViewmodelModifiedChanged(object sender, EventArgs e)
        {
            IsModified = true;
        }

        private void Load()
        {
            foreach (var right in _originalRights.Select(r => new EventRightViewmodel(r)))
            {
                Rights.Add(right);
                right.ModifiedChanged += EventRightViewmodelModifiedChanged;
            }
        }

    }
}
