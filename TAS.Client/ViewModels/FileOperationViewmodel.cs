using System;
using System.ComponentModel;
using System.Windows.Input;
using TAS.Client.Common;
using resources = TAS.Client.Common.Properties.Resources;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    public class FileOperationViewmodel: ViewModelBase
    {
        private bool _isWarning;

        public FileOperationViewmodel(IFileOperationBase fileOperation)
        {
            FileOperation = fileOperation;
            FileOperation.PropertyChanged += OnFileOperationPropertyChanged;
            CommandAbort = new UiCommand(o => FileOperation.Abort(), o => FileOperation.OperationStatus == FileOperationStatus.Waiting || FileOperation.OperationStatus == FileOperationStatus.InProgress);
            CommandShowOutput = new UiCommand(
                o =>
                {
                    var view = new Views.OperationOutputView
                    {
                        DataContext = this,
                        Owner = System.Windows.Application.Current.MainWindow,
                        WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner,
                        ShowInTaskbar = false,
                    };
                    view.ShowDialog();
                }
            );
            CommandShowWarning = new UiCommand(o =>
                System.Windows.MessageBox.Show(OperationWarning, resources._caption_Warning,
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Exclamation)
            );
        }

        public ICommand CommandAbort { get; }

        public ICommand CommandShowOutput { get; }

        public ICommand CommandShowWarning { get; }

        public IFileOperationBase FileOperation { get; }

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

        public bool IsWarning
        {
            get => _isWarning;
            private set => SetField(ref _isWarning, value);
        }

        public string Title
        {
            get
            {
                switch (FileOperation)
                {
                    case IExportOperation exportOperation:
                        return $"{resources._mediaOperation_Export} {string.Join(", ", exportOperation.Sources)} -> {exportOperation.DestDirectory.GetDisplayName()}";
                    case IIngestOperation ingestOperation:
                        return $"{resources._mediaOperation_Ingest} {ingestOperation.Source.Directory.GetDisplayName()}:{ingestOperation.Source.MediaName} -> {ingestOperation.DestDirectory.GetDisplayName()}";
                    case ILoudnessOperation loudnessOperation:
                        return $"{resources._mediaOperation_Loudness} {loudnessOperation.Source.Directory.GetDisplayName()}:{loudnessOperation.Source.MediaName}";
                    case ICopyOperation copyOperation:
                        return $"{resources._mediaOperation_Copy} {copyOperation.Source.Directory.GetDisplayName()}:{copyOperation.Source.MediaName} -> {copyOperation.DestDirectory.GetDisplayName()}";
                    case IMoveOperation moveOperation:
                        return $"{resources._mediaOperation_Move} {moveOperation.Source.Directory.GetDisplayName()}:{moveOperation.Source.MediaName} -> {moveOperation.DestDirectory.GetDisplayName()}";
                    case IDeleteOperation deleteOperation:
                        return $"{resources._mediaOperation_Delete} {deleteOperation.Source.Directory.GetDisplayName()}:{deleteOperation.Source.MediaName}";
                    default:
                        return FileOperation.ToString();
                }
            }
        }

        protected virtual void OnFileOperationPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IFileOperationBase.StartTime) 
                || e.PropertyName == nameof(IFileOperationBase.FinishedTime)
                || e.PropertyName == nameof(IFileOperationBase.TryCount)
                || e.PropertyName == nameof(IFileOperationBase.IsIndeterminate)
                || e.PropertyName == nameof(IFileOperationBase.Progress)
                || e.PropertyName == nameof(IFileOperationBase.OperationStatus)
                || e.PropertyName == nameof(IFileOperationBase.OperationOutput)
                || e.PropertyName == nameof(IFileOperationBase.OperationWarning)
                )
                NotifyPropertyChanged(e.PropertyName);
            if (e.PropertyName == nameof(IFileOperationBase.OperationStatus))
                InvalidateRequerySuggested();
            if (e.PropertyName == nameof(IFileOperationBase.OperationWarning))
                IsWarning = true;
        }

        protected override void OnDispose()
        {
            FileOperation.PropertyChanged -= OnFileOperationPropertyChanged;
        }

    }
}
