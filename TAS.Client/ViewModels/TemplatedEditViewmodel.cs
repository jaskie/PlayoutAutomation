﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Input;
using TAS.Client.Common;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    public class TemplatedEditViewModel : EditViewModelBase<ITemplated>
    {
        private int _templateLayer;
        private TemplateMethod _method;
        private TimeSpan _scheduledDelay;
        private TStartType _startType;
        private bool _bindToEnd;

        public TemplatedEditViewModel(ITemplated model, bool isFieldListReadOnly, bool displayCgMethod, TVideoFormat videoFormat) : base(model)
        {
            IsDisplayCgMethod = displayCgMethod;
            VideoFormat = videoFormat;
            IsFieldListReadOnly = isFieldListReadOnly;
            Model.PropertyChanged += Model_PropertyChanged;
            CommandAddField = new UiCommand(_addField, _canAddField);
            CommandDeleteField = new UiCommand(_deleteField, _canDeleteField);
            CommandEditField = new UiCommand(_editField, _canDeleteField);
        }


        public bool IsDisplayCgMethod { get; }

        public TVideoFormat VideoFormat { get; }

        public int TemplateLayer
        {
            get => _templateLayer;
            set => SetField(ref _templateLayer, value);
        }

        public TimeSpan ScheduledDelay { get => _scheduledDelay; set => SetField(ref _scheduledDelay, value); }

        public TStartType StartType
        {
            get => _startType;
            set
            {
                if (!SetField(ref _startType, value))
                    return;
                _bindToEnd = value == TStartType.WithParentFromEnd;
                NotifyPropertyChanged(nameof(BindToEnd));
            }
        }

        public bool BindToEnd
        {
            get => _bindToEnd;
            set
            {
                if (!SetField(ref _bindToEnd, value)) return;
                if (_startType != TStartType.WithParent && _startType != TStartType.WithParentFromEnd) return;
                StartType = value ? TStartType.WithParentFromEnd : TStartType.WithParent;
            }
        }

        public object SelectedField { get; set; }

        private readonly Dictionary<string, string> _fields = new Dictionary<string, string>();

        public Dictionary<string, string> Fields
        {
            get => new Dictionary<string, string>(_fields);
            set
            {
                _fields.Clear();
                if (value != null)
                    foreach (var keyValuePair in value)
                        _fields.Add(keyValuePair.Key, keyValuePair.Value);
                NotifyPropertyChanged();
            }
        }

        public Array Methods { get; } = Enum.GetValues(typeof(TemplateMethod));

        public TemplateMethod Method
        {
            get => _method;
            set => SetField(ref _method, value);
        }

        public bool IsFieldListReadOnly { get; }

        public ICommand CommandEditField { get; }

        public ICommand CommandAddField { get; }

        public ICommand CommandDeleteField { get; }

        private void Model_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!(sender is ITemplated t))
                return;
            OnUiThread(() =>
            {
               switch (e.PropertyName)
               {
                   case nameof(ITemplated.Method):
                       _method = t.Method;
                       NotifyPropertyChanged(nameof(Method));
                       break;
                   case nameof(ITemplated.TemplateLayer):
                       _templateLayer = t.TemplateLayer;
                       NotifyPropertyChanged(nameof(TemplateLayer));
                       break;
                   case nameof(ITemplated.Fields):
                       _fields.Clear();
                       if (t.Fields != null)
                            foreach (var keyValuePair in t.Fields)
                                _fields.Add(keyValuePair.Key, keyValuePair.Value);
                       NotifyPropertyChanged(nameof(Fields));
                       break;
               }
           });
        }

        protected override void OnDispose()
        {
            Model.PropertyChanged -= Model_PropertyChanged;
        }


        private void _editField(object obj)
        {
            var editObject = obj ?? SelectedField;
            if (editObject == null)
                return;
            using (var kve = new KeyValueEditViewModel((KeyValuePair<string, string>)editObject, false))
            {
                if (WindowManager.Current.ShowDialog(kve) != true
                    || _fields[kve.Key] == kve.Value)
                    return;
                _fields[kve.Key] = kve.Value;
                NotifyPropertyChanged(nameof(Fields));
                IsModified = true;
            }
        }

        private bool _canDeleteField(object obj)
        {
            return SelectedField != null;
        }

        private void _deleteField(object obj)
        {
            if (SelectedField == null)
                return;
            var selected = (KeyValuePair<string, string>)SelectedField;
            _fields.Remove(selected.Key);
            SelectedField = null;
            NotifyPropertyChanged(nameof(Fields));
            IsModified = true;
        }

        private bool _canAddField(object obj)
        {
            return !IsFieldListReadOnly;
        }

        private void _addField(object obj)
        {
            using (var kve = new KeyValueEditViewModel(new KeyValuePair<string, string>(string.Empty, string.Empty), true))
            {
                if (WindowManager.Current.ShowDialog(kve) != true)
                    return;
                _fields.Add(kve.Key, kve.Value);
                NotifyPropertyChanged(nameof(Fields));
                IsModified = true;
            }
        }

        public void Save()
        {
            Update();
        }

        public void UndoEdit()
        {
            Load();
        }
    }
}
