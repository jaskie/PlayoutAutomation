using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows.Data;
using System.Globalization;
using TAS.Common;
using TAS.Server;
using System.Windows.Controls;
using System.Windows;
using System.Collections;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using System.Runtime.Remoting.Messaging;
using System.Diagnostics;
using System.Windows.Input;


namespace TAS.Client.ViewModels
{
    public class MediaManagerViewmodel : ViewmodelBase
    {
        private readonly MediaManager _mediaManager;
        private ICollectionView _mediaView;

        public ICommand CommandSearch { get; private set; }
        public ICommand CommandSaveSelected { get; private set; }
        public ICommand CommandDeleteSelected { get; private set; }
        public ICommand CommandIngestSelectedToServer { get; private set; }
        public ICommand CommandMoveSelectedToArchive { get; private set; }
        public ICommand CommandCopySelectedToArchive { get; private set; }
        public ICommand CommandSweepStaleMedia { get; private set; }
        public ICommand CommandGetLoudness { get; private set; }
        public ICommand CommandRefresh { get; private set; }

        public MediaManagerViewmodel(MediaManager MediaManager, PreviewViewmodel PreviewVm)
        {
            _mediaManager = MediaManager;
            _previewViewModel = PreviewVm;
            _createCommands();
            _mediaCategory = _mediaCategories.FirstOrDefault();
            MediaDirectory = MediaManager.MediaDirectoryPGM;
            EditMedia = new MediaEditViewmodel(PreviewVm);
        }

        private readonly PreviewViewmodel _previewViewModel;
        public PreviewViewmodel PreviewViewModel { get { return _previewViewModel; } }

        private MediaViewViewmodel _selectedMedia;
        public MediaViewViewmodel SelectedMedia 
        { 
            get { return _selectedMedia; }
            set
            {
                if (value != _selectedMedia)
                {
                    Media media = value == null ? null : value.Media;
                    EditMedia.Media = media;
                    if (media is IngestMedia
                        && ((IngestDirectory)media.Directory).AccessType == TDirectoryAccessType.Direct)
                        media.InvokeVerify();
                    PreviewViewModel.Media = media;
                    _selectedMedia = value;
                }
            }
        }
        
        private IList _selectedMediaList;
        public IList SelectedMediaList
        {
            get { return _selectedMediaList; }
            set
            {
                _selectedMediaList = value;
                if (value != null)
                {
                    NotifyPropertyChanged("CommandIngestSelectedToServer");
                    NotifyPropertyChanged("CommandDeleteSelected");
                    NotifyPropertyChanged("CommandMoveSelectedToArchive");
                    NotifyPropertyChanged("CommandCopySelectedToArchive");
                    NotifyPropertyChanged("CommandGetLoudness");
                }
            }
        }

        private List<Media> _getSelections()
        {
            List<Media> ml = new List<Media>();
            if (_selectedMediaList != null)
                foreach (var mediaVm in _selectedMediaList)
                    if (mediaVm is MediaViewViewmodel)
                        ml.Add((mediaVm as MediaViewViewmodel).Media);
            return ml;
        }
        bool _isSomethingSelected(object o)
        {
            return _selectedMediaList != null && _selectedMediaList.Count > 0; 
        }

        bool _canIngestSelectedToServer(object o)
        {
            return (_mediaDirectory is IngestDirectory || _mediaDirectory is ArchiveDirectory) && _isSomethingSelected(o);
        }
        
        public MediaEditViewmodel EditMedia { get; private set; }

        private void _createCommands()
        {
            CommandSearch = new SimpleCommand() { ExecuteDelegate = _search };
            CommandDeleteSelected = new SimpleCommand() { ExecuteDelegate = _deleteSelected, CanExecuteDelegate = _isSomethingSelected };
            CommandMoveSelectedToArchive = new SimpleCommand() { ExecuteDelegate = _moveSelectedToArchive, CanExecuteDelegate = o => _mediaDirectory is ServerDirectory && _isSomethingSelected() };
            CommandCopySelectedToArchive = new SimpleCommand() { ExecuteDelegate = _copySelectedToArchive, CanExecuteDelegate = _isSomethingSelected };
            CommandIngestSelectedToServer = new SimpleCommand() { ExecuteDelegate = _ingestSelectedToServer, CanExecuteDelegate = _canIngestSelectedToServer };

            CommandRefresh = new SimpleCommand()
            {
                ExecuteDelegate = (o) =>
                    {
                        _mediaItems.Clear();
                        (new Action(() => 
                            {
                                try
                                {
                                    MediaDirectory.Refresh();
                                }
                                catch (Exception e)
                                {
                                    MessageBox.Show(string.Format(Properties.Resources._message_DirectoryRefreshError, e.Message), Properties.Resources._caption_Error, MessageBoxButton.OK, MessageBoxImage.Hand);
                                }
                            })).BeginInvoke(ar => ((Action)((AsyncResult)ar).AsyncDelegate).EndInvoke(ar), null);
                    },
                CanExecuteDelegate = (o) =>
                {
                    MediaDirectory dir = _mediaDirectory;
                    return dir is IngestDirectory
                      && ((dir as IngestDirectory).IsXDCAM || (dir as IngestDirectory).AccessType != TDirectoryAccessType.Direct);
                }
            };

            CommandSweepStaleMedia = new SimpleCommand() { ExecuteDelegate = _sweepStaleMedia };
            CommandGetLoudness = new SimpleCommand() { ExecuteDelegate = _getLoudness, CanExecuteDelegate = _isSomethingSelected };
        }

        private void _sweepStaleMedia(object o)
        {
            MediaDirectory dir = MediaDirectory;
            if (dir is ArchiveDirectory)
            {
                (dir as ArchiveDirectory).SweepStaleMedia();
            }
        }

        private void _getLoudness(object o)
        {
            _mediaManager.GetLoudness(_getSelections());
        }

        private void _ingestSelectionToDir(MediaDirectory directory)
        {
            MediaDirectory currentDir = _mediaDirectory;
            if (currentDir is IngestDirectory)
            {
                List<ConvertOperation> ingestList = new List<ConvertOperation>();
                foreach (Media sourceMedia in _getSelections())
                {
                    if (sourceMedia is IngestMedia
                        && ((IngestDirectory)sourceMedia.Directory).AccessType == TDirectoryAccessType.Direct)
                        sourceMedia.InvokeVerify();
                    Media destMedia = null;
                    if (directory is ServerDirectory)
                        destMedia = (directory as ServerDirectory).GetServerMedia(sourceMedia, false);
                    if (directory is ArchiveDirectory)
                        destMedia = (directory as ArchiveDirectory).GetArchiveMedia(sourceMedia, false);
                    if (destMedia != null)
                    {
                        ingestList.Add(new ConvertOperation() {
                            SourceMedia = sourceMedia,
                            DestMedia = destMedia,
                            OutputFormat = _mediaManager.Engine.VideoFormat,
                            AudioVolume = (sourceMedia.Directory is IngestDirectory)? ((IngestDirectory)sourceMedia.Directory).AudioVolume : 0,
                            SourceFieldOrderEnforceConversion = (sourceMedia.Directory is IngestDirectory) ? ((IngestDirectory)sourceMedia.Directory).SourceFieldOrder : TFieldOrder.Unknown,
                            AspectConversion = (sourceMedia.Directory is IngestDirectory) ? ((IngestDirectory)sourceMedia.Directory).AspectConversion : TAspectConversion.NoConversion,
                        });
                    }
                }
                if (ingestList.Count != 0)
                {
                    using (IngestEditViewmodel ievm = new IngestEditViewmodel(ingestList))
                    {
                        IngestEditorView iewnd = new IngestEditorView();
                        iewnd.DataContext = ievm;
                        iewnd.Owner = App.Current.MainWindow;
                        if (iewnd.ShowDialog() == true)
                        {
                            foreach (ConvertOperation operation in ingestList)
                                FileManager.Queue(operation);
                        }
                        else
                            foreach (ConvertOperation operation in ingestList)
                                operation.DestMedia.Remove();
                    }
                }
            }
        }

        private void _ingestSelectedToServer(object o)
        {

            if (EditMedia.Modified)
                switch (MessageBox.Show(Properties.Resources._query_SaveChangedData, Properties.Resources._caption_Confirmation, MessageBoxButton.YesNoCancel))
                {
                    case MessageBoxResult.Cancel:
                        return;
                    case MessageBoxResult.Yes:
                        EditMedia.Save();
                        break;
                    case MessageBoxResult.No:
                        EditMedia.Load();
                        break;
                }
            if (_mediaDirectory is IngestDirectory)
                _ingestSelectionToDir(_mediaManager.MediaDirectoryPGM);
            else
                _mediaManager.AcquireMediaToPlayout(_getSelections(), true);
        }

        private void _search(object o)
        {
            if (_mediaDirectory is ArchiveDirectory)
            {
                (_mediaDirectory as ArchiveDirectory).SearchMediaCategory = _mediaCategory as TMediaCategory?;
                (_mediaDirectory as ArchiveDirectory).SearchString = _searchText;
                (_mediaDirectory as ArchiveDirectory).Search();
            }
            else
            _mediaView.Refresh();
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get { return _searchText; }
            set
            {
                if (value != _searchText)
                {
                    _searchText = value;
                    NotifyPropertyChanged("SearchText");
                }
            }
        }

        private void _deleteSelected(object o)
        {
            List<Media> selection = _getSelections();
            if (MessageBox.Show(string.Format(Properties.Resources._query_DeleteSelectedFiles, string.Join("\n", selection)), Properties.Resources._caption_Confirmation, MessageBoxButton.YesNo)== MessageBoxResult.Yes)
                _mediaManager.DeleteMedia(selection);
        }

        private void _moveSelectedToArchive(object o)
        {
            if (_mediaManager.ArchiveDirectory != null && _mediaDirectory is ServerDirectory)
            {
                    foreach (Media m in _getSelections())
                        _mediaManager.ArchiveMedia(m, true);
            }
        }

        private void _copySelectedToArchive(object o)
        {
            if (_mediaManager.ArchiveDirectory != null)
            {
                if (_mediaDirectory is IngestDirectory)
                    _ingestSelectionToDir(_mediaManager.ArchiveDirectory);
                else
                    foreach (Media m in _getSelections())
                        _mediaManager.ArchiveMedia(m, false);
            }
        }

        private bool FilterOut(object item)
        {
            var m = item as MediaViewViewmodel;
            var searchText = SearchText.ToLower();
            return ((string.IsNullOrEmpty(searchText) || m.MediaName.ToLower().Contains(searchText)) || m.FileName.ToLower().Contains(_searchText))
               && (_mediaCategory as TMediaCategory? == null || m.MediaCategory == (TMediaCategory)_mediaCategory);
        }

        readonly IEnumerable<object> _mediaCategories = (new List<object>(){Properties.Resources._all_}).Concat(Enum.GetValues(typeof(TMediaCategory)).Cast<object>());
        public IEnumerable<object> MediaCategories { get { return _mediaCategories; } }

        private object _mediaCategory;
        public object MediaCategory
        {
            get { return _mediaCategory; }
            set
            {
                if (_mediaCategory != value)
                {
                    _mediaCategory = value;
                    _search(null);
                }
            }
        }

        public List<MediaDirectory> MediaDirectories { get { return _mediaManager.Directories(); } }

        private MediaDirectory _mediaDirectory;

        public MediaDirectory MediaDirectory
        {
            get { return _mediaDirectory; }
            set 
            {
                if (_mediaDirectory != value)
                {
                    if (_mediaDirectory != null)
                    {
                        _mediaDirectory.MediaAdded -= MediaAdded;
                        _mediaDirectory.MediaRemoved -= MediaRemoved;
                        _mediaDirectory.PropertyChanged -= MediaDirectoryPropertyChanged;
                    }
                    _mediaDirectory = value;
                    if (_mediaDirectory != null)
                    {
                        value.MediaAdded += MediaAdded;
                        value.MediaRemoved += MediaRemoved;
                        value.PropertyChanged += MediaDirectoryPropertyChanged;
                        if (value is ArchiveDirectory)
                            if (!string.IsNullOrEmpty((value as ArchiveDirectory).SearchString))
                                SearchText = (value as ArchiveDirectory).SearchString;
                    }
                    _reloadFiles();
                    SelectedMedia = null;
                    NotifyPropertyChanged("MediaDirectory");
                    NotifyPropertyChanged("DisplayDirectoryInfo");
                    NotifyPropertyChanged("CommandRefresh");
                    _notifyDirectoryPropertiesChanged();
                }
            }
        }

        private void MediaDirectoryPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SearchString")
            {
                if (sender is ArchiveDirectory)
                    SearchText = (sender as ArchiveDirectory).SearchString;
            }
            if (e.PropertyName == "IsInitialized" && (sender as MediaDirectory).IsInitialized)
            {
            Application.Current.Dispatcher.BeginInvoke((Action)delegate() {_reloadFiles();});
            }
            if (e.PropertyName == "VolumeFreeSize")
                _notifyDirectoryPropertiesChanged();
        }
        private void _reloadFiles()
        {
            _mediaItems = new ObservableCollection<MediaViewViewmodel>();
            IEnumerable<MediaViewViewmodel> itemsToLoad;
            if (_mediaDirectory is ServerDirectory)
                itemsToLoad = _mediaDirectory.Files.Where(f => (f.MediaType == TMediaType.Movie || f.MediaType == TMediaType.Still)).Select(f => new MediaViewViewmodel(f));
            else
                itemsToLoad = _mediaDirectory.Files.Select(f => new MediaViewViewmodel(f));
            foreach (MediaViewViewmodel mvm in itemsToLoad)
                _mediaItems.Add(mvm);
            _mediaView = CollectionViewSource.GetDefaultView(_mediaItems);
            _mediaView.SortDescriptions.Add(new SortDescription("MediaName", ListSortDirection.Ascending));
            if (!(_mediaDirectory is ArchiveDirectory))
                _mediaView.Filter = new Predicate<object>(FilterOut);
            System.Threading.Tasks.Task.Factory.StartNew(_mediaDirectory.Refresh);
            NotifyPropertyChanged("MediaItems");
        }

        private bool _isSomethingSelected()
        {
            return _selectedMediaList != null && _selectedMediaList.Count > 0;
        }

        private void MediaAdded(object source, MediaEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)delegate()
            {
                if (!(MediaDirectory is ServerDirectory) || e.Media.MediaType == TMediaType.Movie || e.Media.MediaType == TMediaType.Still)
                _mediaItems.Add(new MediaViewViewmodel(e.Media));
            }
                , null);
        }

        private void MediaRemoved(object source, MediaEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)delegate()
            {
                var vm = _mediaItems.FirstOrDefault(v => v.Media == e.Media);
                if (vm != null)
                    _mediaItems.Remove(vm);
            }, null);
        }

        private void _notifyDirectoryPropertiesChanged()
        {
            NotifyPropertyChanged("DirectoryFreeOver20Percent");
            NotifyPropertyChanged("DirectoryFreePercentage");
            NotifyPropertyChanged("DirectoryTotalSpace");
            NotifyPropertyChanged("DirectoryFreeSpace");
        }

        public bool DisplayDirectoryInfo { get { return _mediaDirectory != null && _mediaDirectory.AccessType == TDirectoryAccessType.Direct; } }
        public bool DirectoryFreeOver20Percent { get { return (DirectoryFreeSpace >= 80); } }
        public float DirectoryTotalSpace { get { return _mediaDirectory == null ? 0F : _mediaDirectory.VolumeTotalSize / (1073741824F); } }
        public float DirectoryFreeSpace { get { return _mediaDirectory == null ? 0F : _mediaDirectory.VolumeFreeSize / (1073741824F); } }
        public float DirectoryFreePercentage
        {
            get
            {
                UInt64 totalSize = _mediaDirectory == null ? 0 : _mediaDirectory.VolumeTotalSize;
                return (totalSize == 0) ? 0F : _mediaDirectory.VolumeFreeSize * 100F / _mediaDirectory.VolumeTotalSize;
            }
        }

        protected override void OnDispose()
        {
        }

        private ObservableCollection<MediaViewViewmodel> _mediaItems;

        public ObservableCollection<MediaViewViewmodel> MediaItems { get { return _mediaItems; } }

    }


}
