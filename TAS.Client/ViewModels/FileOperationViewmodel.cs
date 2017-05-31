using System;
using System.ComponentModel;
using System.Windows.Input;
using TAS.Client.Common;
using resources = TAS.Client.Common.Properties.Resources;
using TAS.Server.Common;
using TAS.Server.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    public class FileOperationViewmodel: ViewmodelBase
    {
        private bool _isWarning;

        public FileOperationViewmodel(IFileOperation fileOperation)
        {
            FileOperation = fileOperation;
            FileOperation.PropertyChanged += OnFileOperationPropertyChanged;
            CommandAbort = new UICommand() { ExecuteDelegate = o => FileOperation.Abort(), CanExecuteDelegate = o => FileOperation.OperationStatus == FileOperationStatus.Waiting || FileOperation.OperationStatus == FileOperationStatus.InProgress };
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

        public ICommand CommandAbort { get; }

        public ICommand CommandShowOutput { get; }

        public ICommand CommandShowWarning { get; }

        public IFileOperation FileOperation { get; }

        public int Progress => FileOperation.Progress;

        public DateTime ScheduledTime => FileOperation.ScheduledTime;

        public DateTime StartTime => FileOperation.StartTime;

        public DateTime FinishedTime => FileOperation.FinishedTime;

        public int TryCount => FileOperation.TryCount;

        public bool IsIndeterminate => FileOperation.IsIndeterminate;

        public bool Finished => FileOperation.OperationStatus == FileOperationStatus.Failed || FileOperation.OperationStatus == FileOperationStatus.Aborted || FileOperation.OperationStatus == FileOperationStatus.Finished;

        public FileOperationStatus OperationStatus => FileOperation.OperationStatus;

        public string OperationOutput => string.Join(Environment.NewLine, FileOperation.OperationOutput);

        public string OperationWarning => FileOperation.OperationWarning.AsString(Environment.NewLine);

        public bool IsWarning { get { return _isWarning; } private set { SetField(ref _isWarning, value); } }

        public string Title => FileOperation.Title;

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

        protected override void OnDispose()
        {
            FileOperation.PropertyChanged -= OnFileOperationPropertyChanged;
        }


    }
}
