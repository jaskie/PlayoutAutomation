using System;
using System.Linq;
using System.Collections.ObjectModel;
using TAS.Common;
using System.Windows.Input;
using TAS.Client.Common;
using System.Windows.Threading;
using TAS.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    public class FileManagerViewmodel : ViewModelBase
    {
        private readonly IFileManager _fileManager;
        private readonly IMediaManager _mediaManager;
        private bool _clearFinished;

        public FileManagerViewmodel(IMediaManager mediaManager)
        {
            _mediaManager = mediaManager;
            _fileManager = mediaManager.FileManager;
            _fileManager.OperationAdded += FileManager_OperationAdded;
            _fileManager.OperationCompleted += FileManager_OperationCompleted;
            OperationList = new ObservableCollection<FileOperationViewmodel>(_fileManager.GetOperationQueue().Select(fo => new FileOperationViewmodel(fo, mediaManager)));
            CommandClearFinished = new UiCommand(CommandName(nameof(ClearFinishedOperations)), ClearFinishedOperations, _ => OperationList.Any(o => o.Finished));
            CommandCancelPending = new UiCommand(CommandName(nameof(IFileManager.CancelPending)), _ => _fileManager.CancelPending(), _ => OperationList.Any(o => o.OperationStatus == FileOperationStatus.Waiting));
            DispatcherTimer clearTimer = new DispatcherTimer();
            clearTimer.Tick += (o, e) =>
            {
                foreach (FileOperationViewmodel vm in OperationList.Where(op => op.FileOperation.OperationStatus == FileOperationStatus.Finished && op.FinishedTime > DateTime.Now+TimeSpan.FromHours(1)).ToList())
                {
                    OperationList.Remove(vm);
                    vm.Dispose();
                }
            };
            clearTimer.Interval = TimeSpan.FromMinutes(10);
            clearTimer.Start();
        }

        public ICommand CommandClearFinished { get; }
        public ICommand CommandCancelPending { get; }

        public ObservableCollection<FileOperationViewmodel> OperationList { get; }

        public bool ClearFinished
        {
            get => _clearFinished;
            set
            {
                if (SetField(ref _clearFinished, value) && value)
                    ClearFinishedOperations(null);
            }
        }

        private void FileManager_OperationCompleted(object sender, FileOperationEventArgs e)
        {
            if (e.Operation == null)
                return;
            OnUiThread(() =>
            {
                if (_clearFinished && e.Operation.OperationStatus != FileOperationStatus.Failed && (e.Operation.OperationWarning?.Count ?? 0) == 0) // don't remove failed or with warnings
                {
                    FileOperationViewmodel
                        fovm = OperationList.FirstOrDefault(vm =>
                            vm.FileOperation == e.Operation); 
                    if (fovm != null)
                    {
                        OperationList.Remove(fovm);
                        fovm.Dispose();
                    }
                }
                InvalidateRequerySuggested();
            });
        }

        private void FileManager_OperationAdded(object sender, FileOperationEventArgs e)
        {
            if (e.Operation == null)
                return;
            OnUiThread(() =>
            {
                OperationList.Insert(0, new FileOperationViewmodel(e.Operation, _mediaManager));
            });
        }

        private void ClearFinishedOperations(object _)
        {
            foreach (FileOperationViewmodel vm in OperationList.Where(f => f.Finished).ToList())
            {
                OperationList.Remove(vm);
                vm.Dispose();
            }
        }

        protected override void OnDispose()
        {
            _fileManager.OperationAdded -= FileManager_OperationAdded;
            _fileManager.OperationCompleted -= FileManager_OperationCompleted;
        }

    }
}
