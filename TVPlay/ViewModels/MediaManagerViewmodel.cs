using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows.Data;
using System.Globalization;
using System.Windows;
using System.Collections;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using System.Runtime.Remoting.Messaging;
using System.Diagnostics;
using System.Windows.Input;
using System.Threading;
using TAS.Common;
using TAS.Client.Common;
using TAS.Server;
using resources = TAS.Client.Common.Properties.Resources;


namespace TAS.Client.ViewModels
{
    public class MediaManagerViewmodel : ViewmodelBase
    {
        private readonly MediaManager _mediaManager;
        private readonly MediaManagerView _view;
        private ICollectionView _mediaView;

        public ICommand CommandSearch { get; private set; }
        public ICommand CommandSaveSelected { get; private set; }
        public ICommand CommandDeleteSelected { get; private set; }
        public ICommand CommandIngestSelectedToServer { get; private set; }
        public ICommand CommandMoveSelectedToArchive { get; private set; }
        public ICommand CommandCopySelectedToArchive { get; private set; }
        public ICommand CommandSweepStaleMedia { get; private set; }
        public ICommand CommandGetLoudness { get; private set; }
        public ICommand CommandExport { get; private set; }
        public ICommand CommandRefresh { get; private set; }

        public MediaManagerViewmodel(MediaManager MediaManager, PreviewViewmodel previewVm)
        {
            _mediaManager = MediaManager;
            _previewViewModel = previewVm;
            _previewView = new PreviewView(previewVm.FrameRate) { DataContext = previewVm };
            _createCommands();
            _mediaCategory = _mediaCategories.FirstOrDefault();
            MediaDirectory = MediaManager.MediaDirectoryPGM;
            _view = new MediaManagerView() { DataContext = this };
        }


        private readonly PreviewViewmodel _previewViewModel;
        private readonly PreviewView _previewView;
        public PreviewView PreviewView { get { return _previewView; } }

        public MediaManagerView View { get { return _view; } }

        private MediaViewViewmodel _selectedMedia;
        public MediaViewViewmodel SelectedMedia 
        { 
            get { return _selectedMedia; }
            set
            {
                var oldEditMedia = _editMedia;
                if (oldEditMedia != null
                    && oldEditMedia.Modified)
                {
                    switch (MessageBox.Show(Properties.Resources._query_SaveChangedData, resources._caption_Confirmation, MessageBoxButton.YesNo))
                    {
                        case MessageBoxResult.Yes:
                            oldEditMedia.Save();
                            break;
                        case MessageBoxResult.No:
                            oldEditMedia.Revert();
                            break;
                    }
                }
                if (SetField(ref _selectedMedia, value, "SelectedMedia"))
                {
                    Media media = value == null ? null : value.Media;
                    if (media is IngestMedia
                        && ((IngestDirectory)media.Directory).AccessType == TDirectoryAccessType.Direct
                        && !media.Verified)
                        ThreadPool.QueueUserWorkItem(new WaitCallback( o => media.Verify()));
                    _previewViewModel.Media = media;
                    EditMedia = _selectedMedia == null ? null : new MediaEditViewmodel(_selectedMedia.Media, _previewViewModel, true);
                }
            }
        }

        private MediaEditViewmodel _editMedia;
        public MediaEditViewmodel EditMedia
        {
            get { return _editMedia; }
            set
            {
                var _oldMedia = _editMedia;
                if (SetField(ref _editMedia, value, "EditMedia") && _oldMedia != null)
                    _oldMedia.Dispose();
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
                    NotifyPropertyChanged("CommandExport");
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

        bool _canExport(object o)
        {
            return (_mediaDirectory is ServerDirectory || _mediaDirectory is ArchiveDirectory) && _isSomethingSelected(o) && _mediaManager.IngestDirectories.Any(d => d.IsXDCAM);
        }
        
        private void _createCommands()
        {
            CommandSearch = new UICommand() { ExecuteDelegate = _search };
            CommandDeleteSelected = new UICommand() { ExecuteDelegate = _deleteSelected, CanExecuteDelegate = _isSomethingSelected };
            CommandMoveSelectedToArchive = new UICommand() { ExecuteDelegate = _moveSelectedToArchive, CanExecuteDelegate = o => _mediaDirectory is ServerDirectory && _isSomethingSelected() };
            CommandCopySelectedToArchive = new UICommand() { ExecuteDelegate = _copySelectedToArchive, CanExecuteDelegate = _isSomethingSelected };
            CommandIngestSelectedToServer = new UICommand() { ExecuteDelegate = _ingestSelectedToServer, CanExecuteDelegate = _canIngestSelectedToServer };

            CommandRefresh = new UICommand()
            {
                ExecuteDelegate = (ob) =>
                    {
                        ThreadPool.QueueUserWorkItem(o =>
                            {
                                try
                                {
                                    MediaDirectory.Refresh();
                                }
                                catch (Exception e)
                                {
                                    MessageBox.Show(string.Format(resources._message_CommandFailed, e.Message), resources._caption_Error, MessageBoxButton.OK, MessageBoxImage.Hand);
                                }
                            });
                    },
                CanExecuteDelegate = (o) =>
                {
                    MediaDirectory dir = _mediaDirectory;
                    return dir is IngestDirectory
                      && ((dir as IngestDirectory).IsXDCAM || (dir as IngestDirectory).AccessType != TDirectoryAccessType.Direct);
                }
            };

            CommandSweepStaleMedia = new UICommand() { ExecuteDelegate = _sweepStaleMedia };
            CommandGetLoudness = new UICommand() { ExecuteDelegate = _getLoudness, CanExecuteDelegate = _isSomethingSelected };
            CommandExport = new UICommand() { ExecuteDelegate = _export, CanExecuteDelegate = _canExport };
        }

        private void _export(object obj)
        {
            var selections = _getSelections().Select( m => new MediaExport(m, m.TCPlay, m.DurationPlay, m.AudioVolume));
            using (ExportViewmodel evm = new ExportViewmodel(this._mediaManager, selections)) { }
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
                        && ((IngestDirectory)sourceMedia.Directory).AccessType == TDirectoryAccessType.Direct
                        && !sourceMedia.Verified)
                        ThreadPool.QueueUserWorkItem(o => sourceMedia.Verify());
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
                        IngestEditorView iewnd = new IngestEditorView()
                        {
                            DataContext = ievm,
                            Owner = App.Current.MainWindow,
                            ShowInTaskbar = false
                        };
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
                switch (MessageBox.Show(Properties.Resources._query_SaveChangedData, resources._caption_Confirmation, MessageBoxButton.YesNoCancel))
                {
                    case MessageBoxResult.Cancel:
                        return;
                    case MessageBoxResult.Yes:
                        EditMedia.Save();
                        break;
                    case MessageBoxResult.No:
                        EditMedia.Revert();
                        break;
                }
            if (_mediaDirectory is IngestDirectory)
                _ingestSelectionToDir(_mediaManager.MediaDirectoryPGM);
            else
                _mediaManager.IngestMediaToPlayout(_getSelections(), true);
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
                if (_mediaDirectory is IngestDirectory && ((IngestDirectory)_mediaDirectory).IsWAN)
                    ((IngestDirectory)_mediaDirectory).Filter = _searchText;
                else
                    _mediaView.Refresh();
            NotifyPropertyChanged("ItemsCount");
        }

        private string[] _searchTextSplit = new string[0];
        private string _searchText = string.Empty;
        public string SearchText
        {
            get { return _searchText; }
            set
            {
                if (SetField(ref _searchText, value, "SearchText"))
                    _searchTextSplit = value.ToLower().Split(' ');
            }
        }

        private void _deleteSelected(object o)
        {
            List<Media> selection = _getSelections();
            if (MessageBox.Show(string.Format(Properties.Resources._query_DeleteSelectedFiles, selection.AsString(Environment.NewLine, 20)), resources._caption_Confirmation, MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                var reasons = _mediaManager.DeleteMedia(selection).Where(r => r.Reason != MediaDeleteDeny.MediaDeleteDenyReason.NoDeny);
                if (reasons.Any())
                {
                    foreach (var reason in reasons)
                    {
                        string reasonMsg = reason.Reason == MediaDeleteDeny.MediaDeleteDenyReason.MediaInFutureSchedule ? string.Format(resources._message_MediaDeleteDenyReason_Scheduled, reason.Event == null ? resources._unknown_ : reason.Event.EventName, reason.Event == null ? resources._unknown_ : reason.Event.ScheduledTime.ToLocalTime().ToString())
                            : resources._message_MediaDeleteDenyReason_Unknown;
                        MessageBox.Show(string.Format(resources._message_MediaDeleteNotAllowed, reason.Media.MediaName, reasonMsg), resources._caption_Error, MessageBoxButton.OK);
                    }
                }
            }
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

        private bool _filter(object item)
        {
            if (_mediaDirectory is ArchiveDirectory
                || (_mediaDirectory is IngestDirectory && ((IngestDirectory)_mediaDirectory).IsWAN))
                return true;
            var m = item as MediaViewViewmodel;
            string mediaName = m.MediaName == null ? string.Empty:  m.MediaName.ToLower();
            return (_mediaCategory as TMediaCategory? == null || m.MediaCategory == (TMediaCategory)_mediaCategory)
               && (_searchTextSplit.All(s => mediaName.Contains(s)));;
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
                    if (value != null)
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
                Application.Current.Dispatcher.BeginInvoke((Action)delegate() {_reloadFiles();});
            if (e.PropertyName == "VolumeFreeSize")
                _notifyDirectoryPropertiesChanged();
        }
        private void _reloadFiles()
        {
            if (_mediaItems != null)
            {
                foreach (var m in _mediaItems)
                    m.Dispose();
            }
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
                _mediaView.Filter = new Predicate<object>(_filter);
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
                if (!(MediaDirectory is ServerDirectory) || (e.Media.MediaType == TMediaType.Movie || e.Media.MediaType == TMediaType.Still))
                {
                    _mediaItems.Add(new MediaViewViewmodel(e.Media));
                    _mediaView.Refresh();
                    _notifyDirectoryPropertiesChanged();
                }
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
                _notifyDirectoryPropertiesChanged();
            }, null);
        }

        private void _notifyDirectoryPropertiesChanged()
        {
            NotifyPropertyChanged("DirectoryFreeOver20Percent");
            NotifyPropertyChanged("DirectoryFreePercentage");
            NotifyPropertyChanged("DirectoryTotalSpace");
            NotifyPropertyChanged("DirectoryFreeSpace");
            NotifyPropertyChanged("ItemsCount");
        }

        public bool DisplayDirectoryInfo { get { return _mediaDirectory != null && _mediaDirectory.AccessType == TDirectoryAccessType.Direct; } }
        public bool DirectoryFreeOver20Percent { get { return (DirectoryFreePercentage >= 20); } }
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

        public int ItemsCount { get { return _mediaItems == null ? 0 : _mediaItems.Where(m => _filter(m)).Count(); } }

        protected override void OnDispose()
        {
        }

        private ObservableCollection<MediaViewViewmodel> _mediaItems;

        public ObservableCollection<MediaViewViewmodel> MediaItems { get { return _mediaItems; } }

    }


}
