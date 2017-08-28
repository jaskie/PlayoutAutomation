using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Windows;
using TAS.Common;
using System.Windows.Input;
using TAS.Client.Common;
using System.Windows.Threading;
using TAS.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    public class FileManagerViewmodel : ViewmodelBase
    {
        private readonly IFileManager _fileManager;

        public FileManagerViewmodel(IFileManager fileManager)
        {
            _fileManager = fileManager;
            fileManager.OperationAdded += FileManager_OperationAdded;
            fileManager.OperationCompleted += FileManager_OperationCompleted;
            OperationList = new ObservableCollection<FileOperationViewmodel>(fileManager.GetOperationQueue().Select(fo => new FileOperationViewmodel(fo)));
            CommandClearFinished = new UICommand { ExecuteDelegate = _clearFinishedOperations, CanExecuteDelegate = o => OperationList.Any(op => op.Finished) };
            CommandCancelPending = new UICommand { ExecuteDelegate = o => _fileManager.CancelPending(), CanExecuteDelegate = o => OperationList.Any(op => op.OperationStatus == FileOperationStatus.Waiting) };
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
            get { return _clearFinished; }
            set
            {
                if (SetField(ref _clearFinished, value) && value)
                    _clearFinishedOperations(null);
            }
        }

        internal IConvertOperation CreateConvertOperation(IMedia sourceMedia, IMediaProperties destMediaProperties, IMediaDirectory destDirectory, TVideoFormat outputFormat, decimal audioVolume, TFieldOrder sourceFieldOrderEnforceConversion, TAspectConversion aspectConversion, bool loudnessCheck)
        {
            IConvertOperation result = _fileManager.CreateConvertOperation();
            result.Source = sourceMedia;
            result.DestProperties = destMediaProperties;
            result.DestDirectory = destDirectory;
            result.AudioVolume = audioVolume;
            result.SourceFieldOrderEnforceConversion = sourceFieldOrderEnforceConversion;
            result.AspectConversion = aspectConversion;
            result.StartTC = sourceMedia.TcPlay;
            result.Duration = sourceMedia.DurationPlay;
            result.LoudnessCheck = loudnessCheck;
            return result;
        }

        private void FileManager_OperationCompleted(object sender, FileOperationEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)(() =>
            {
                if (_clearFinished)
                {
                    FileOperationViewmodel fovm = OperationList.FirstOrDefault(vm => vm.FileOperation == e.Operation);
                    if (fovm != null)
                    {
                        OperationList.Remove(fovm);
                        fovm.Dispose();
                    }
                }
                InvalidateRequerySuggested();
            }), null);
        }

        private void FileManager_OperationAdded(object sender, FileOperationEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)(() =>
            {
                OperationList.Insert(0, new FileOperationViewmodel(e.Operation));
            })
                , null);
        }

        private void _clearFinishedOperations(object parameter)
        {
            foreach (FileOperationViewmodel vm in OperationList.Where(f => f.Finished).ToList())
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
