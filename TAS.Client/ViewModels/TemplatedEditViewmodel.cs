using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using TAS.Client.Common;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    public class TemplatedEditViewmodel : EditViewmodelBase<ITemplated>
    {
        private int _templateLayer;
        private TemplateMethod _method;
        private TimeSpan _scheduledDelay;
        private TStartType _startType;
        private bool _bindToEnd;

        public TemplatedEditViewmodel(ITemplated model, bool isFieldListReadOnly, bool displayCgMethod, TVideoFormat videoFormat) : base(model)
        {
            IsDisplayCgMethod = displayCgMethod;
            VideoFormat = videoFormat;
            IsFieldListReadOnly = isFieldListReadOnly;
            Model.PropertyChanged += TemplatedEditViewmodel_PropertyChanged;
            CommandAddField = new UICommand { ExecuteDelegate = _addField, CanExecuteDelegate = _canAddField };
            CommandDeleteField = new UICommand { ExecuteDelegate = _deleteField, CanExecuteDelegate = _canDeleteField };
            CommandEditField = new UICommand { ExecuteDelegate = _editField, CanExecuteDelegate = _canDeleteField };
        }


        public bool IsDisplayCgMethod { get; } = true;

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

        private readonly ObservableDictionary<string, string> _fields = new ObservableDictionary<string, string>();

        public Dictionary<string, string> Fields
        {
            get => new Dictionary<string, string>(_fields);
            set
            {
                _fields.Clear();
                if (value != null)
                    _fields.AddRange(value);
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

        private void TemplatedEditViewmodel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!(sender is ITemplated t))
                return;
            Application.Current?.Dispatcher.BeginInvoke((Action)delegate
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
                           _fields.AddRange(t.Fields);
                       NotifyPropertyChanged(nameof(Fields));
                       break;
               }
           });
        }

        protected override void OnDispose()
        {
            Model.PropertyChanged -= TemplatedEditViewmodel_PropertyChanged;
        }


        private void _editField(object obj)
        {
            var editObject = obj ?? SelectedField;
            if (editObject == null)
                return;
            using (var kve = new KeyValueEditViewmodel((KeyValuePair<string, string>)editObject, false))
            {
                if (UiServices.ShowDialog<Views.KeyValueEditView>(kve) != true
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
            using (var kve = new KeyValueEditViewmodel(new KeyValuePair<string, string>(string.Empty, string.Empty), true))
            {
                if (UiServices.ShowDialog<Views.KeyValueEditView>(kve) != true)
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
