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
    public class FileManagerViewModel : ViewModelBase
    {
        private readonly IFileManager _fileManager;
        private readonly IMediaManager _mediaManager;

        public FileManagerViewModel(IMediaManager mediaManager)
        {
            _mediaManager = mediaManager;
            _fileManager = mediaManager.FileManager;
            _fileManager.OperationAdded += FileManager_OperationAdded;
            _fileManager.OperationCompleted += FileManager_OperationCompleted;
            OperationList = new ObservableCollection<FileOperationViewModel>(_fileManager.GetOperationQueue().Select(fo => new FileOperationViewModel(fo, mediaManager)));
            CommandClearFinished = new UiCommand(_clearFinishedOperations, o => OperationList.Any(op => op.Finished));
            CommandCancelPending = new UiCommand(o => _fileManager.CancelPending(), o => OperationList.Any(op => op.OperationStatus == FileOperationStatus.Waiting));
            DispatcherTimer clearTimer = new DispatcherTimer();
            clearTimer.Tick += (o, e) =>
            {
                foreach (FileOperationViewModel vm in OperationList.Where(op => op.FileOperation.OperationStatus == FileOperationStatus.Finished && op.FinishedTime > DateTime.Now+TimeSpan.FromHours(1)).ToList())
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

        public ObservableCollection<FileOperationViewModel> OperationList { get; }

        public bool ClearFinished
        {
            get => _clearFinished;
            set
            {
                if (SetField(ref _clearFinished, value) && value)
                    _clearFinishedOperations(null);
            }
        }

        private void FileManager_OperationCompleted(object sender, FileOperationEventArgs e)
        {
            if (e.Operation == null)
                return;
            OnUiThread(() =>
            {
                if (_clearFinished && e.Operation.OperationStatus != FileOperationStatus.Failed)
                {
                    FileOperationViewModel
                        fovm = OperationList.FirstOrDefault(vm =>
                            vm.FileOperation == e.Operation); // don't remove failed
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
                OperationList.Insert(0, new FileOperationViewModel(e.Operation, _mediaManager));
            });
        }

        private void _clearFinishedOperations(object parameter)
        {
            foreach (FileOperationViewModel vm in OperationList.Where(f => f.Finished).ToList())
            {
                OperationList.Remove(vm);
                vm.Dispose();
            }
        }

        private bool _clearFinished;

        protected override void OnDispose()
        {
            _fileManager.OperationAdded -= FileManager_OperationAdded;
            _fileManager.OperationCompleted -= FileManager_OperationCompleted;
        }

    }
}
