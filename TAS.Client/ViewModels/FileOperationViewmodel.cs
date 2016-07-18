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
            _fileOperation = fileOperation;
            _fileOperation.PropertyChanged += OnFileOperationPropertyChanged;
            CommandAbort = new UICommand() { ExecuteDelegate = o => _fileOperation.Abort(), CanExecuteDelegate = o => _fileOperation.OperationStatus == FileOperationStatus.Waiting || _fileOperation.OperationStatus == FileOperationStatus.InProgress };
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
            _fileOperation.PropertyChanged -= OnFileOperationPropertyChanged;
        }

        public int Progress { get { return _fileOperation.Progress; } }
        public DateTime ScheduledTime { get { return _fileOperation.ScheduledTime; } }
        public DateTime StartTime { get { return _fileOperation.StartTime; } }
        public DateTime FinishedTime { get { return _fileOperation.FinishedTime; } }
        public int TryCount { get { return _fileOperation.TryCount; } }
        public bool IsIndeterminate { get { return _fileOperation.IsIndeterminate; } }

        public bool Finished { get { return _fileOperation.OperationStatus == FileOperationStatus.Failed || _fileOperation.OperationStatus == FileOperationStatus.Aborted || _fileOperation.OperationStatus == FileOperationStatus.Finished; } }

        public FileOperationStatus OperationStatus { get { return _fileOperation.OperationStatus; } }
        public string OperationOutput { get { return string.Join(Environment.NewLine, _fileOperation.OperationOutput); } }
        public string OperationWarning { get { return _fileOperation.OperationWarning.AsString(Environment.NewLine); } }
        private bool _isWarning;
        public bool IsWarning { get { return _isWarning; } set { SetField(ref _isWarning, value, nameof(IsWarning)); } }
        public string Title { get { return _fileOperation.Title; } }

        public ICommand CommandAbort { get; private set; }
        public ICommand CommandShowOutput { get; private set; }
        public ICommand CommandShowWarning { get; private set; }

        protected virtual void OnFileOperationPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IFileOperation.StartTime) 
                || e.PropertyName == nameof(IFileOperation.FinishedTime)
                || e.PropertyName == nameof(IFileOperation.TryCount)
                || e.PropertyName == nameof(IFileOperation.IsIndeterminate)
                || e.PropertyName == nameof(IFileOperation.Progress)
                || e.PropertyName == nameof(IFileOperation.OperationStatus)
                || e.PropertyName == nameof(IFileOperation.OperationOutput)
                || e.PropertyName == nameof(IFileOperation.OperationWarning)
                || e.PropertyName == nameof(IFileOperation.Title)
                )
                NotifyPropertyChanged(e.PropertyName);
            if (e.PropertyName == nameof(IFileOperation.OperationStatus))
                InvalidateRequerySuggested();
            if (e.PropertyName == nameof(IFileOperation.OperationWarning))
                IsWarning = true;
        }
        public override string ToString()
        {
            return _fileOperation.Title;
        }

    }
}
