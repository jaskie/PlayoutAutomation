using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Windows;
using System.ComponentModel;
using TAS.Server.Interfaces;
using TAS.Client.Common;
using resources = TAS.Client.Common.Properties.Resources;
using System.Windows.Input;

namespace TAS.Client.ViewModels
{
    class IngestEditViewmodel : OkCancelViewmodelBase<IList<IConvertOperation>>
    {
        private readonly ObservableCollection<ConvertOperationViewModel> _conversionList;
        public IngestEditViewmodel(IList<IConvertOperation> convertionList, IPreview preview, IMediaManager mediaManager): base(convertionList, new Views.IngestEditorView(), resources._window_IngestAs)
        {
            _conversionList = new ObservableCollection<ConvertOperationViewModel>(from op in convertionList select new ConvertOperationViewModel(op, preview, mediaManager));
            SelectedOperation = _conversionList.FirstOrDefault();
            foreach (var c in _conversionList)
                c.PropertyChanged += _convertOperationPropertyChanged;
            CommandDeleteOperation = new UICommand { ExecuteDelegate = _deleteOperation };
            OkCancelButtonsActivateViaKeyboard = false;
        }

        private void _deleteOperation(object obj)
        {
            var operation = obj as ConvertOperationViewModel;
            int operaionIndex = _conversionList.IndexOf(operation);
            if (OperationList.Remove(operation))
            {
                operation.PropertyChanged -= _convertOperationPropertyChanged;
                operation.Dispose();
                OnModified();
                SelectedOperation = _conversionList[Math.Min(_conversionList.Count - 1, operaionIndex)];
                NotifyPropertyChanged(nameof(ShowMediaList));
            }
        }

        void _convertOperationPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ConvertOperationViewModel.IsValid))
                OnModified();
        }

        public ObservableCollection<ConvertOperationViewModel> OperationList { get { return _conversionList; } }

        private ConvertOperationViewModel _selectedOperation;
        public ConvertOperationViewModel SelectedOperation
        {
            get { return _selectedOperation; }
            set { SetField(ref _selectedOperation, value, nameof(SelectedOperation)); }
        }

        public ICommand CommandDeleteOperation { get; private set; }

        public bool ShowMediaList
        {
            get { return _conversionList.Count > 1; }
        }

        protected override void OnDispose()
        {
            foreach (var c in _conversionList)
            {
                c.PropertyChanged -= _convertOperationPropertyChanged;
                c.Dispose();
            }
        }
        
        public bool IsValid
        {
            get
            {
                foreach (ConvertOperationViewModel mediaVm in _conversionList)
                {
                    if (!mediaVm.IsValid)
                        return false;
                    if (_conversionList.Count(c => c.DestFileName == mediaVm.DestFileName) > 1)
                        return false;
                }
                return true;
            }
        }

        protected override bool CanOK(object parameter)
        {
            return IsValid;
        }

        protected override void Ok(object o)
        {
            foreach (ConvertOperationViewModel c in _conversionList)
                c.Apply();
            base.Ok(o);
        }


    }
}

