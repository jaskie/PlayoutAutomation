using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using System.ComponentModel;
using TAS.Client.Common;
using TAS.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    internal class IngestEditorViewmodel : ModifyableViewModelBase
    {
        private readonly IEngine _engine;
        private IngestOperationViewModel _selectedOperation;

        public IngestEditorViewmodel(IList<IIngestOperation> convertionList, IEngine engine)
        {
            _engine = engine;
            OperationList = new ObservableCollection<IngestOperationViewModel>(convertionList.Select(op => new IngestOperationViewModel(op, engine)));
            SelectedOperation = OperationList.FirstOrDefault();
            foreach (var c in OperationList)
            {
                c.PropertyChanged += Operation_PropertyChanged;
                c.Removed += Operation_Removed;
            }
        }



        public void ScheduleAll()
        {
            foreach (var c in OperationList)
            {
                c.Apply();
                _engine.MediaManager?.FileManager?.Queue(c.FileOperation);
            }
        }

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
                foreach (var operation in OperationList)
                {
                    if (!operation.IsValid)
                        return false;
                    if (OperationList.Count(c => c.DestFileName == operation.DestFileName) > 1)
                        return false;
                }
                return true;
            }
        }

        private void Operation_Removed(object sender, EventArgs e)
        {
            if (!(sender is IngestOperationViewModel operation))
                return;
            var operaionIndex = OperationList.IndexOf(operation);
            if (!OperationList.Remove(operation)) return;
            operation.PropertyChanged -= Operation_PropertyChanged;
            operation.Removed -= Operation_Removed;
            operation.Dispose();
            OnModifiedChanged();
            SelectedOperation = OperationList[Math.Min(OperationList.Count - 1, operaionIndex)];
            NotifyPropertyChanged(nameof(ShowMediaList));
            NotifyPropertyChanged(nameof(IsValid));
        }

        void Operation_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IngestOperationViewModel.IsValid))
                NotifyPropertyChanged(nameof(IsValid));
        }

        protected override void OnDispose()
        {
            foreach (var c in OperationList)
            {
                c.PropertyChanged -= Operation_PropertyChanged;
                c.Removed -= Operation_Removed;
                c.Dispose();
            }
            OperationList.Clear();
        }

    }
}

