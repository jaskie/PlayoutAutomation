using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using TAS.Client.Common;
using System.Windows.Input;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    internal class IngestEditorViewmodel : ViewmodelBase
    {
        private IngestOperationViewModel _selectedOperation;

        public IngestEditorViewmodel(IList<IIngestOperation> convertionList, IPreview preview, IMediaManager mediaManager)
        {
            OperationList = new ObservableCollection<IngestOperationViewModel>(convertionList.Select(op =>
            {
                string destFileName =
                    $"{Path.GetFileNameWithoutExtension(op.Source.FileName)}{FileUtils.DefaultFileExtension(op.Source.MediaType)}";
                IPersistentMediaProperties destMediaProperties = new PersistentMediaProxy
                {
                    FileName = op.DestDirectory.GetUniqueFileName(destFileName),
                    MediaName = FileUtils.GetFileNameWithoutExtension(destFileName, op.Source.MediaType),
                    MediaType = op.Source.MediaType == TMediaType.Unknown ? TMediaType.Movie : op.Source.MediaType,
                    Duration = op.Source.Duration,
                    TcStart = op.StartTC,
                    MediaGuid = op.Source.MediaGuid,
                    MediaCategory = op.Source.MediaCategory
                };
                return new IngestOperationViewModel(op, destMediaProperties, preview, mediaManager);
            }));
            SelectedOperation = OperationList.FirstOrDefault();
            foreach (var c in OperationList)
                c.PropertyChanged += _convertOperationPropertyChanged;
            CommandDeleteOperation = new UICommand {ExecuteDelegate = _deleteOperation};
            CommandOk = new UICommand {ExecuteDelegate = _ok, CanExecuteDelegate = _canOk};
        }

        private void _ok(object obj)
        {
            foreach (IngestOperationViewModel c in OperationList)
                c.Apply();
        }

        private bool _canOk(object obj)
        {
            return IsValid;
        }

        public ICommand CommandDeleteOperation { get; }
        public ICommand CommandOk { get; }

        public ObservableCollection<IngestOperationViewModel> OperationList { get; }

        public IngestOperationViewModel SelectedOperation
        {
            get { return _selectedOperation; }
            set { SetField(ref _selectedOperation, value); }
        }

        public bool ShowMediaList => OperationList.Count > 1;

        public bool IsValid
        {
            get
            {
                foreach (IngestOperationViewModel mediaVm in OperationList)
                {
                    if (!mediaVm.IsValid)
                        return false;
                    if (OperationList.Count(c => c.DestFileName == mediaVm.DestFileName) > 1)
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
                OnModified();
                SelectedOperation = OperationList[Math.Min(OperationList.Count - 1, operaionIndex)];
                NotifyPropertyChanged(nameof(ShowMediaList));
            }
        }

        void _convertOperationPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IngestOperationViewModel.IsValid))
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

