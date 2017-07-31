using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using System.ComponentModel;
using TAS.Client.Common;
using resources = TAS.Client.Common.Properties.Resources;
using System.Windows.Input;
using TAS.Server.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    internal class IngestEditViewmodel : OkCancelViewmodelBase<IList<IConvertOperation>>
    {
        private ConvertOperationViewModel _selectedOperation;

        public IngestEditViewmodel(IList<IConvertOperation> convertionList, IPreview preview, IMediaManager mediaManager): base(convertionList, typeof(Views.IngestEditorView), resources._window_IngestAs)
        {
            OperationList = new ObservableCollection<ConvertOperationViewModel>(from op in convertionList select new ConvertOperationViewModel(op, preview, mediaManager));
            SelectedOperation = OperationList.FirstOrDefault();
            foreach (var c in OperationList)
                c.PropertyChanged += _convertOperationPropertyChanged;
            CommandDeleteOperation = new UICommand { ExecuteDelegate = _deleteOperation };
            OkCancelButtonsActivateViaKeyboard = false;
        }

        public ICommand CommandDeleteOperation { get; }

        public ObservableCollection<ConvertOperationViewModel> OperationList { get; }

        public ConvertOperationViewModel SelectedOperation
        {
            get { return _selectedOperation; }
            set { SetField(ref _selectedOperation, value); }
        }

        public bool ShowMediaList => OperationList.Count > 1;

        public bool IsValid
        {
            get
            {
                foreach (ConvertOperationViewModel mediaVm in OperationList)
                {
                    if (!mediaVm.IsValid)
                        return false;
                    if (OperationList.Count(c => c.DestFileName == mediaVm.DestFileName) > 1)
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
            foreach (ConvertOperationViewModel c in OperationList)
                c.Apply();
            base.Ok(o);
        }

        private void _deleteOperation(object obj)
        {
            var operation = obj as ConvertOperationViewModel;
            if (operation == null)
                return;
            int operaionIndex = OperationList.IndexOf(operation);
            if (OperationList.Remove(operation))
            {
                operation.PropertyChanged -= _convertOperationPropertyChanged;
                operation.Dispose();
                OnModified();
                SelectedOperation = OperationList[Math.Min(OperationList.Count - 1, operaionIndex)];
                NotifyPropertyChanged(nameof(ShowMediaList));
            }
        }

        void _convertOperationPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ConvertOperationViewModel.IsValid))
                OnModified();
        }

        protected override void OnDispose()
        {
            foreach (var c in OperationList)
            {
                c.PropertyChanged -= _convertOperationPropertyChanged;
                c.Dispose();
            }
            OperationList.Clear();
        }


    }
}

