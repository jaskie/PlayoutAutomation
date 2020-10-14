using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using TAS.Client.Common;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.MediaDirectory;

namespace TAS.Client.ViewModels
{
    public class ExportViewModel : ViewModelBase
    {
        private readonly IEngine _engine;
        private MediaDirectoryViewModel _selectedDirectory;
        private bool _concatMedia;
        private string _concatMediaName;
        private TmXFAudioExportFormat _mXFAudioExportFormat;
        private TmXFVideoExportFormat _mXFVideoExportFormat;

        public ExportViewModel(IEngine engine, IEnumerable<MediaExportDescription> exportList)
        {
            _engine = engine;
            Items = new ObservableCollection<ExportMediaViewModel>(exportList.Select(media => new ExportMediaViewModel(engine, media)));
            Directories = engine.MediaManager.IngestDirectories.Where(d => d.ContainsExport()).Select(d => new MediaDirectoryViewModel(d, d.DirectoryName, false, true)).ToList();
            SelectedDirectory = Directories.FirstOrDefault();
            CommandExport = new UiCommand(_export, _canExport);
        }

        public ICommand CommandExport { get; }

        public List<MediaDirectoryViewModel> Directories { get; }
        
        public MediaDirectoryViewModel SelectedDirectory
        {
            get => _selectedDirectory;
            set
            {
                if (!SetField(ref _selectedDirectory, value))
                    return;
                NotifyPropertyChanged(nameof(IsConcatMediaNameVisible));
                NotifyPropertyChanged(nameof(IsXDCAM));
                NotifyPropertyChanged(nameof(IsMXF));
                if (value?.ExportContainerFormat == TMovieContainerFormat.mxf 
                    || value?.IsXdcam == true)
                {
                    MXFAudioExportFormat = value.MXFAudioExportFormat;
                    MXFVideoExportFormat = value.MXFVideoExportFormat;
                }
                InvalidateRequerySuggested();
            }
        }

        public ObservableCollection<ExportMediaViewModel> Items { get; }

        public bool IsXDCAM => _selectedDirectory?.IsXdcam == true;

        public bool IsMXF => _selectedDirectory?.ExportContainerFormat == TMovieContainerFormat.mxf || _selectedDirectory?.IsXdcam == true;

        public bool ConcatMedia
        {
            get => _concatMedia;
            set
            {
                if (SetField(ref _concatMedia, value))
                {
                    NotifyPropertyChanged(nameof(IsConcatMediaNameVisible));
                }
            }
        }

        public string ConcatMediaName
        {
            get => _concatMediaName;
            set
            {
                if (SetField(ref _concatMediaName, value))
                    InvalidateRequerySuggested();
            }
        }

        public bool IsConcatMediaNameVisible => _concatMedia && !IsXDCAM;

        public Array MXFVideoExportFormats { get; } = Enum.GetValues(typeof(TmXFVideoExportFormat));

        public Array MXFAudioExportFormats { get; } = Enum.GetValues(typeof(TmXFAudioExportFormat));

        public TmXFAudioExportFormat MXFAudioExportFormat { get => _mXFAudioExportFormat; set => SetField(ref _mXFAudioExportFormat, value); }

        public TmXFVideoExportFormat MXFVideoExportFormat { get => _mXFVideoExportFormat; set => SetField(ref _mXFVideoExportFormat, value); }

        public bool CanConcatMedia => Items.Count > 1;

        public int ExportMediaCount => Items.Count;

        public TimeSpan TotalTime { get { return TimeSpan.FromTicks(Items.Sum(m => m.Duration.Ticks)); } }


        private void _export (object o)
        {
            _checking = true;
            try
            {
                //TODO: check if exporting files fit in device free space
            }
            finally
            {
                _checking = false;
                InvalidateRequerySuggested();
            }
            _engine.MediaManager?.Export(Items.Select(mevm => mevm.MediaExport).ToArray(), _concatMedia, _concatMediaName, (IIngestDirectory)SelectedDirectory.Directory, _mXFAudioExportFormat, _mXFVideoExportFormat);
        }

        private bool _checking;
        private bool _canExport(object o)
        {
            return !_checking && Items.Count > 0
                && SelectedDirectory.IsExport
                && (!IsConcatMediaNameVisible || !string.IsNullOrWhiteSpace(_concatMediaName));
        }
        
        protected override void OnDispose()
        {
        }
    }
}
