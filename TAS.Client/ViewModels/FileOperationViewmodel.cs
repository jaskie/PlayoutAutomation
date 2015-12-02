using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows.Input;
using TAS.Client.Common;
using resources = TAS.Client.Common.Properties.Resources;
using TAS.Common;
using TAS.Server.Interfaces;

namespace TAS.Client.ViewModels
{
    public class FileOperationViewmodel: ViewmodelBase
    {
        private readonly IFileOperation _fileOperation;

        public FileOperationViewmodel(IFileOperation fileOperation)
        {
            fileOperation.ReferenceAdd();
            _fileOperation = fileOperation;
            _fileOperation.PropertyChanged += OnPropertyChanged;
            CommandAbort = new UICommand() { ExecuteDelegate = o => _fileOperation.Aborted = true, CanExecuteDelegate = o => _fileOperation.OperationStatus == FileOperationStatus.Waiting || _fileOperation.OperationStatus == FileOperationStatus.InProgress };
            CommandShowOutput = new UICommand()
            {
                ExecuteDelegate = o =>
                {
                    Views.OperationOutputView view = new Views.OperationOutputView 
                    { 
                        DataContext = this, 
                        Owner = System.Windows.Application.Current.MainWindow, 
                        WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner,
                        ShowInTaskbar = false,
                    };
                    view.ShowDialog();
                }
            };
            CommandShowWarning = new UICommand()
            {
                ExecuteDelegate = o =>
                    System.Windows.MessageBox.Show(OperationWarning, resources._caption_Warning, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Exclamation)
            };
        }

        public IFileOperation FileOperation { get { return _fileOperation; } }

        protected override void OnDispose()
        {
            _fileOperation.PropertyChanged -= OnPropertyChanged;
            _fileOperation.ReferenceRemove();
        }

        public int Progress { get { return _fileOperation.Progress; } }
        public DateTime ScheduledTime { get { return _fileOperation.ScheduledTime.ToLocalTime(); } }
        public DateTime StartTime { get { return _fileOperation.ScheduledTime.ToLocalTime(); } }
        public DateTime FinishedTime { get { return _fileOperation.FinishedTime.ToLocalTime(); } }
        public int TryCount { get { return _fileOperation.TryCount; } }
        public bool IsIndeterminate { get { return _fileOperation.IsIndeterminate; } }

        public bool Finished { get { return _fileOperation.OperationStatus != FileOperationStatus.Waiting && _fileOperation.OperationStatus != FileOperationStatus.InProgress; } }

        public FileOperationStatus OperationStatus { get { return _fileOperation.OperationStatus; } }
        public string OperationOutput { get { return string.Join(Environment.NewLine, _fileOperation.OperationOutput); } }
        public string OperationWarning { get { return _fileOperation.OperationWarning.AsString(Environment.NewLine); } }
        private bool _isWarning;
        public bool IsWarning { get { return _isWarning; } set { SetField(ref _isWarning, value, "IsWarning"); } }
        public string Title { get { return _fileOperation.Title; } }

        public ICommand CommandAbort { get; private set; }
        public ICommand CommandShowOutput { get; private set; }
        public ICommand CommandShowWarning { get; private set; }

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
                || e.PropertyName == "OperationWarning"
                )
                NotifyPropertyChanged(e.PropertyName);
            if (e.PropertyName == "OperationStatus")
                NotifyPropertyChanged("CommandAbort");
            if (e.PropertyName == "OperationWarning")
                IsWarning = true;
        }
        public override string ToString()
        {
            return _fileOperation.Title;
        }

    }
}
