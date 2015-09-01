using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Server;
using System.ComponentModel;
using System.Windows.Input;
using TAS.Common;

namespace TAS.Client.ViewModels
{
    public class FileOperationViewmodel: ViewmodelBase
    {
        private readonly FileOperation _fileOperation;
        public FileOperationViewmodel(FileOperation fileOperation)
        {
            _fileOperation = fileOperation;
            _fileOperation.PropertyChanged += OnPropertyChanged;
            CommandAbort = new SimpleCommand() { ExecuteDelegate = o => _fileOperation.Aborted = true, CanExecuteDelegate = o => _fileOperation.OperationStatus == FileOperationStatus.Waiting || _fileOperation.OperationStatus == FileOperationStatus.InProgress };
            CommandShowOutput = new SimpleCommand()
            {
                ExecuteDelegate = o =>
                {
                    Views.OperationOutputView view = new Views.OperationOutputView { DataContext = this };
                    view.ShowDialog();
                }
            };
        }

        protected override void OnDispose()
        {
            _fileOperation.PropertyChanged -= OnPropertyChanged;
        }

        public bool IsFileOperation(FileOperation operation)
        {
            return _fileOperation == operation;
        }

        public string OperationDescription
        {
            get { return _fileOperation.ToString(); }
        }

        public int Progress { get { return _fileOperation.Progress; } }
        public DateTime ScheduledTime { get { return _fileOperation.ScheduledTime; } }
        public DateTime StartTime { get { return _fileOperation.StartTime; } }
        public DateTime FinishedTime { get { return _fileOperation.FinishedTime; } }
        public int TryCount { get { return _fileOperation.TryCount; } }
        public bool IsIndeterminate { get { return _fileOperation.IsIndeterminate; } }

        public bool Finished { get { return _fileOperation.OperationStatus != FileOperationStatus.Waiting && _fileOperation.OperationStatus != FileOperationStatus.InProgress; } }

        public FileOperationStatus OperationStatus { get { return _fileOperation.OperationStatus; } }
        public string OperationOutput { get { return string.Join(Environment.NewLine, _fileOperation.OperationOutput.ToList()); } }

        public ICommand CommandAbort { get; private set; }
        public ICommand CommandShowOutput { get; private set; }

        protected virtual void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "StartTime" 
                || e.PropertyName == "FinishedTime" 
                || e.PropertyName == "CompletedSuccessfully" 
                || e.PropertyName == "TryCount" 
                || e.PropertyName == "IsIndeterminate" 
                || e.PropertyName == "Progress"
                || e.PropertyName == "OperationStatus"
                || e.PropertyName == "OperationOutput"
                )
                NotifyPropertyChanged(e.PropertyName);
            if (e.PropertyName == "OperationStatus")
                NotifyPropertyChanged("CommandAbort");
        }


    }
}
