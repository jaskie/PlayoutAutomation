using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using System.ComponentModel;
using TAS.Client.Common;
using System.Windows.Input;
using TAS.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    internal class IngestEditorViewmodel : ModifyableViewModelBase
    {
        private readonly IEngine _engine;
        private IngestOperationViewModel _selectedOperation;

        public IngestEditorViewmodel(IList<IIngestOperation> convertionList, IPreview preview, IEngine engine)
        {
            _engine = engine;
            OperationList = new ObservableCollection<IngestOperationViewModel>(convertionList.Select(op => new IngestOperationViewModel(op, preview, engine)));
            SelectedOperation = OperationList.FirstOrDefault();
            foreach (var c in OperationList)
                c.PropertyChanged += _convertOperationPropertyChanged;
            CommandDeleteOperation = new UiCommand(_deleteOperation);
        }

        public void ScheduleAll()
        {
            foreach (IngestOperationViewModel c in OperationList)
            {
                c.Apply();
                _engine.MediaManager?.FileManager?.Queue(c.FileOperation, false);
            }
        }

        public ICommand CommandDeleteOperation { get; }
        
        public ObservableCollection<IngestOperationViewModel> OperationList { get; }

        public IngestOperationViewModel SelectedOperation
        {
            get => _selectedOperation;
            set => SetField(ref _selectedOperation, value);
        }

        public bool ShowMediaList => OperationList.Count > 1;

        public bool IsValid
        {
            get
            {
                foreach (IngestOperationViewModel operation in OperationList)
                {
                    if (!operation.IsValid)
                        return false;
                    if (OperationList.Count(c => c.DestFileName == operation.DestFileName) > 1)
                        return false;
                }
                return true;
            }
        }
        

        private void _deleteOperation(object obj)
        {
            if (!(obj is IngestOperationViewModel operation))
                return;
            int operaionIndex = OperationList.IndexOf(operation);
            if (OperationList.Remove(operation))
            {
                operation.PropertyChanged -= _convertOperationPropertyChanged;
                operation.Dispose();
                OnModifiedChanged();
                SelectedOperation = OperationList[Math.Min(OperationList.Count - 1, operaionIndex)];
                NotifyPropertyChanged(nameof(ShowMediaList));
            }
        }

        void _convertOperationPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IngestOperationViewModel.IsValid))
                NotifyPropertyChanged(nameof(IsValid));
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

