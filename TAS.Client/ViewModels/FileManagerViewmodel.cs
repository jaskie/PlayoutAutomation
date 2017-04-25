using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows;
using TAS.Server.Interfaces;
using TAS.Server.Common;
using TAS.Common;
using System.Windows.Input;
using TAS.Client.Common;
using System.Windows.Threading;

namespace TAS.Client.ViewModels
{
    public class FileManagerViewmodel : ViewmodelBase
    {
        private ObservableCollection<FileOperationViewmodel> _operationList;
        private readonly IFileManager _fileManager;
        public ICommand CommandClearFinished { get; private set; }
        public ICommand CommandCancelPending { get; private set; }

        public FileManagerViewmodel(IFileManager fileManager)
        {
            _fileManager = fileManager;
            fileManager.OperationAdded += FileManager_OperationAdded;
            fileManager.OperationCompleted += FileManager_OperationCompleted;
            _operationList = new ObservableCollection<FileOperationViewmodel>(fileManager.GetOperationQueue().Select(fo => new FileOperationViewmodel(fo)));
            CommandClearFinished = new UICommand { ExecuteDelegate = _clearFinishedOperations, CanExecuteDelegate = o => _operationList.Any(op => op.Finished) };
            CommandCancelPending = new UICommand { ExecuteDelegate = o => _fileManager.CancelPending(), CanExecuteDelegate = o => _operationList.Any(op => op.OperationStatus == FileOperationStatus.Waiting) };
            DispatcherTimer clearTimer = new DispatcherTimer();
            clearTimer.Tick += (o, e) =>
            {
                foreach (FileOperationViewmodel vm in _operationList.Where(op => op.FileOperation.OperationStatus == FileOperationStatus.Finished && op.FinishedTime > DateTime.Now+TimeSpan.FromHours(1)).ToList())
                {
                    _operationList.Remove(vm);
                    vm.Dispose();
                }
            };
            clearTimer.Interval = TimeSpan.FromMinutes(10);
            clearTimer.Start();
        }

        private void FileManager_OperationCompleted(object sender, FileOperationEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)(() =>
            {
                if (_clearFinished)
                {
                    FileOperationViewmodel fovm = _operationList.FirstOrDefault(vm => vm.FileOperation == e.Operation);
                    if (fovm != null)
                    {
                        _operationList.Remove(fovm);
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
                _operationList.Insert(0, new FileOperationViewmodel(e.Operation));
            })
                , null);
        }

        public ObservableCollection<FileOperationViewmodel> OperationList { get { return _operationList; } }
        
        private void _clearFinishedOperations(object parameter)
        {
            foreach (FileOperationViewmodel vm in _operationList.Where(f => f.Finished).ToList())
            {
                _operationList.Remove(vm);
                vm.Dispose();
            }
        }

        private bool _clearFinished;
        public bool ClearFinished
        {
            get { return _clearFinished; }
            set
            {
                if (SetField(ref _clearFinished, value) && value)
                    _clearFinishedOperations(null);
            }
        }

        protected override void OnDispose()
        {
            _fileManager.OperationAdded -= FileManager_OperationAdded;
            _fileManager.OperationCompleted -= FileManager_OperationCompleted;
        }

        internal IConvertOperation CreateConvertOperation(IMedia sourceMedia, IMediaProperties destMediaProperties, IMediaDirectory destDirectory, TVideoFormat outputFormat, decimal audioVolume, TFieldOrder sourceFieldOrderEnforceConversion, TAspectConversion aspectConversion, bool loudnessCheck)
        {
            IConvertOperation result = _fileManager.CreateConvertOperation();
            result.SourceMedia = sourceMedia;
            result.DestMediaProperties = destMediaProperties;
            result.DestDirectory = destDirectory;
            result.AudioVolume = audioVolume;
            result.SourceFieldOrderEnforceConversion = sourceFieldOrderEnforceConversion;
            result.AspectConversion = aspectConversion;
            result.StartTC = sourceMedia.TcPlay;
            result.Duration = sourceMedia.DurationPlay;
            result.LoudnessCheck = loudnessCheck;
            return result;
        }

    }
}
