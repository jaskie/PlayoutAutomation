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
using resources = TAS.Client.Common.Properties.Resources;
using TAS.Server.Common;
using TAS.Server.Interfaces;
using TAS.Client.Views;

namespace TAS.Client.ViewModels
{
    public class MediaManagerViewmodel : ViewmodelBase
    {
        private readonly IMediaManager _mediaManager;
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
        public ICommand CommandSyncPriToSec { get; private set; }

        public MediaManagerViewmodel(IMediaManager mediaManager, IPreview preview)
        {
            _mediaManager = mediaManager;
            if (preview != null)
            {
                _previewViewModel = new PreviewViewmodel(preview);
                _previewView = new PreviewView(_previewViewModel.FrameRate) { DataContext = _previewViewModel };
            }
            _createCommands();

            _mediaDirectories = new List<IMediaDirectory>();
            _mediaDirectories.AddRange(mediaManager.IngestDirectories.Where(d => d.IsImport));
            IArchiveDirectory archiveDirectory = mediaManager.ArchiveDirectory;
            if (archiveDirectory != null)
                _mediaDirectories.Insert(0, archiveDirectory);
            IServerDirectory serverDirectoryPRI = mediaManager.MediaDirectoryPRI;
            if (serverDirectoryPRI != null)
                _mediaDirectories.Insert(0, serverDirectoryPRI);
            IServerDirectory serverDirectorySEC = mediaManager.MediaDirectorySEC;
            if (serverDirectorySEC != null && serverDirectorySEC != serverDirectoryPRI)
                _mediaDirectories.Insert(1, serverDirectorySEC);

            _mediaCategory = _mediaCategories.FirstOrDefault();
            MediaDirectory = mediaManager.MediaDirectoryPRI;
            _view = new MediaManagerView() { DataContext = this };
            if (mediaManager.FileManager != null)
                _fileManagerVm = new FileManagerViewmodel(mediaManager.FileManager);
        }


        private readonly PreviewViewmodel _previewViewModel;
        private readonly PreviewView _previewView;
        public PreviewView PreviewView { get { return _previewView; } }

        public MediaManagerView View { get { return _view; } }

        private readonly FileManagerViewmodel _fileManagerVm;
        public FileManagerViewmodel FileManagerVm { get { return _fileManagerVm; } }

        bool _previewDisplay;
        public bool PreviewDisplay { get { return _previewDisplay; } set { SetField(ref _previewDisplay, value, "PreviewDisplay"); } }

        private MediaViewViewmodel _selectedMedia;
        public MediaViewViewmodel SelectedMedia 
        { 
            get { return _selectedMedia; }
            set
            {
                if (!_checkEditMediaSaved())
                {
                    NotifyPropertyChanged("SelectedMedia");
                    return;
                }
                var oldSelectedMedia = _selectedMedia;
                if (SetField(ref _selectedMedia, value, "SelectedMedia"))
                {
                    if (oldSelectedMedia != null)
                        oldSelectedMedia.SelectedSegment = null;
                    IMedia media = value == null ? null : value.Media;
                    if (media is IIngestMedia
                        && ((IIngestDirectory)media.Directory).AccessType == TDirectoryAccessType.Direct
                        && !media.Verified)
                        media.ReVerify();
                    if (_previewViewModel != null)
                        _previewViewModel.Media = media;
                    EditMedia = _selectedMedia == null ? null : new MediaEditViewmodel(_selectedMedia.Media, _mediaManager, _previewViewModel, true);
                }
            }
        }

        private MediaEditViewmodel _editMedia;
        public MediaEditViewmodel EditMedia
        {
            get { return _editMedia; }
            set
            {
                MediaEditViewmodel oldEditMedia = _editMedia;
                if (SetField(ref _editMedia, value, "EditMedia") && oldEditMedia != null)
                    oldEditMedia.Dispose();
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

        private List<IMedia> _getSelections()
        {
            List<IMedia> ml = new List<IMedia>();
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
            return (_mediaDirectory is IIngestDirectory || _mediaDirectory is IArchiveDirectory) && _isSomethingSelected(o);
        }

        bool _canExport(object o)
        {
            return (_mediaDirectory is IServerDirectory || _mediaDirectory is IArchiveDirectory) && _isSomethingSelected(o) && _mediaManager.IngestDirectories.Any(d => d.IsExport);
        }
        
        private void _createCommands()
        {
            CommandSearch = new UICommand() { ExecuteDelegate = _search, CanExecuteDelegate = _canSearch };
            CommandDeleteSelected = new UICommand() { ExecuteDelegate = _deleteSelected, CanExecuteDelegate = _isSomethingSelected };
            CommandMoveSelectedToArchive = new UICommand() { ExecuteDelegate = _moveSelectedToArchive, CanExecuteDelegate = o => _mediaDirectory is IServerDirectory && _isSomethingSelected(o) };
            CommandCopySelectedToArchive = new UICommand() { ExecuteDelegate = _copySelectedToArchive, CanExecuteDelegate = o => _mediaDirectory is IServerDirectory && _isSomethingSelected(o) };
            CommandIngestSelectedToServer = new UICommand() { ExecuteDelegate = _ingestSelectedToServer, CanExecuteDelegate = _canIngestSelectedToServer };

            CommandRefresh = new UICommand()
            {
                ExecuteDelegate = (ob) =>
                    {
                        ThreadPool.QueueUserWorkItem(o =>
                            {
                                _refreshMediaDirectory(MediaDirectory);
                            });
                    },
                CanExecuteDelegate = (o) =>
                {
                    IMediaDirectory dir = _mediaDirectory;
                    return dir is IIngestDirectory
                      && ((dir as IIngestDirectory).IsXDCAM || (dir as IIngestDirectory).AccessType != TDirectoryAccessType.Direct);
                }
            };

            CommandSweepStaleMedia = new UICommand() { ExecuteDelegate = _sweepStaleMedia };
            CommandGetLoudness = new UICommand() { ExecuteDelegate = _getLoudness, CanExecuteDelegate = _isSomethingSelected };
            CommandExport = new UICommand() { ExecuteDelegate = _export, CanExecuteDelegate = _canExport };
            CommandSyncPriToSec = new UICommand { ExecuteDelegate = _syncSecToPri, CanExecuteDelegate = o => _mediaDirectory is IServerDirectory };
        }

        private void _refreshMediaDirectory(IMediaDirectory directory)
        {
            try
            {
                directory.Refresh();
            }
            catch (Exception e)
            {
                if (directory == MediaDirectory)
                    Application.Current.Dispatcher.BeginInvoke((Action)delegate ()
                    {
                        MessageBox.Show(string.Format(resources._message_DirectoryRefreshFailed, e.Message), resources._caption_Error, MessageBoxButton.OK, MessageBoxImage.Hand);
                    });
            }
        }

        private void _syncSecToPri(object o)
        {
            if (_mediaDirectory is IServerDirectory)
                ThreadPool.QueueUserWorkItem((obj)=>
                    _mediaManager.SynchronizeSecToPri(true));
        }

        private void _export(object obj)
        {
            var selections = _getSelections().Select( m => new ExportMedia(m, new List<IMedia>(), m.TcPlay, m.DurationPlay, m.AudioVolume));
            using (ExportViewmodel evm = new ExportViewmodel(this._mediaManager, selections)) { }
        }

        private void _sweepStaleMedia(object o)
        {
            MediaDirectory.SweepStaleMedia();
        }

        private void _getLoudness(object o)
        {
            _mediaManager.GetLoudness(_getSelections());
        }

        private void _ingestSelectionToDir(IServerDirectory directory)
        {
            IMediaDirectory currentDir = _mediaDirectory;
            if (currentDir is IIngestDirectory)
            {
                List<IConvertOperation> ingestList = new List<IConvertOperation>();
                foreach (IMedia sourceMedia in _getSelections())
                {
                    if (sourceMedia is IIngestMedia
                        && ((IIngestDirectory)sourceMedia.Directory).AccessType == TDirectoryAccessType.Direct
                        && !sourceMedia.Verified)
                            sourceMedia.ReVerify();
                    IMedia destMedia = null;
                    destMedia = (directory as IServerDirectory).GetServerMedia(sourceMedia, false);
                    if (destMedia != null)
                    {
                        destMedia.Duration = sourceMedia.DurationPlay;
                        destMedia.TcStart = sourceMedia.TcPlay;
                        ingestList.Add(
                            FileManagerVm.CreateConvertOperation(
                            sourceMedia,
                            destMedia,
                            _mediaManager.VideoFormat,
                            (sourceMedia.Directory is IIngestDirectory)? ((IIngestDirectory)sourceMedia.Directory).AudioVolume : 0,
                            (sourceMedia.Directory is IIngestDirectory) ? ((IIngestDirectory)sourceMedia.Directory).SourceFieldOrder : TFieldOrder.Unknown,
                            (sourceMedia.Directory is IIngestDirectory) ? ((IIngestDirectory)sourceMedia.Directory).AspectConversion : TAspectConversion.NoConversion
                            ));
                    }
                }
                if (ingestList.Count != 0)
                {
                    using (IngestEditViewmodel ievm = new IngestEditViewmodel(ingestList))
                    {
                        if (ievm.ShowDialog() == true)
                        {
                            foreach (IConvertOperation operation in ingestList)
                                _mediaManager.FileManager.Queue(operation, false);
                        }
                        else
                            foreach (IConvertOperation operation in ingestList)
                                operation.DestMedia.Delete();
                    }
                }
            }
        }

        private bool _checkEditMediaSaved()
        {
            if (EditMedia != null && EditMedia.Modified)
                switch (MessageBox.Show(resources._query_SaveChangedData, resources._caption_Confirmation, MessageBoxButton.YesNoCancel))
                {
                    case MessageBoxResult.Cancel:
                        return false;
                    case MessageBoxResult.Yes:
                        EditMedia.Save();
                        break;
                    case MessageBoxResult.No:
                        EditMedia.Revert();
                        break;
                }
            return true;
        }

        private void _ingestSelectedToServer(object o)
        {
            if (!_checkEditMediaSaved())
                return;
            IServerDirectory pri = _mediaManager.MediaDirectoryPRI;
            IServerDirectory sec = _mediaManager.MediaDirectorySEC;
            IServerDirectory dir = pri != null && pri.DirectoryExists() ? pri : sec != null && sec.DirectoryExists() ? sec : null;
            if (dir != null)
            {
                if (_mediaDirectory is IIngestDirectory)
                    _ingestSelectionToDir(dir);
                else
                    _mediaManager.CopyMediaToPlayout(_getSelections(), true);
            }
        }

        private bool _canSearch(object o)
        {
            return (_mediaDirectory is IServerDirectory 
                || (_mediaDirectory is IIngestDirectory && !((IIngestDirectory)_mediaDirectory).IsWAN)
                || _searchText.Length >= 3);
        }

        private void _search(object o)
        {
            if (_mediaDirectory is IArchiveDirectory)
            {
                (_mediaDirectory as IArchiveDirectory).SearchMediaCategory = _mediaCategory as TMediaCategory?;
                (_mediaDirectory as IArchiveDirectory).SearchString = _searchText;
                (_mediaDirectory as IArchiveDirectory).Search();
            }
            else
                if (_mediaDirectory is IIngestDirectory && ((IIngestDirectory)_mediaDirectory).IsWAN)
                    ((IIngestDirectory)_mediaDirectory).Filter = _searchText;
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
            List<IMedia> selection = _getSelections();
            if (MessageBox.Show(string.Format(resources._query_DeleteSelectedFiles, selection.AsString(Environment.NewLine, 20)), resources._caption_Confirmation, MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                var reasons = _mediaManager.DeleteMedia(selection).Where(r => r.Reason != MediaDeleteDenyReason.MediaDeleteDenyReasonEnum.NoDeny);
                if (reasons.Any())
                {
                    StringBuilder reasonMsg = new StringBuilder();
                    foreach (var reason in reasons)
                    {
                        switch (reason.Reason)
                        {
                            case MediaDeleteDenyReason.MediaDeleteDenyReasonEnum.NoDeny:
                                break;
                            case MediaDeleteDenyReason.MediaDeleteDenyReasonEnum.MediaInFutureSchedule:
                                reasonMsg.AppendLine().Append(reason.Media.MediaName).Append(": ").AppendFormat(resources._message_MediaDeleteDenyReason_Scheduled, reason.Event == null ? resources._unknown_ : reason.Event.EventName, reason.Event == null ? resources._unknown_ : reason.Event.ScheduledTime.ToLocalTime().ToString());
                                break;
                            case MediaDeleteDenyReason.MediaDeleteDenyReasonEnum.Protected:
                                reasonMsg.AppendLine().Append(reason.Media.MediaName).Append(": ").Append(resources._message_MediaDeleteDenyReason_Protected);
                                break;
                            default:
                                reasonMsg.AppendLine().Append(reason.Media.MediaName).Append(": ").Append(resources._message_MediaDeleteDenyReason_Unknown);
                                break;
                        }
                    }
                    if (reasonMsg.Length > 0)
                        MessageBox.Show(String.Join(Environment.NewLine, resources._message_MediaDeleteNotAllowed, reasonMsg.ToString()), resources._caption_Error, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void _moveSelectedToArchive(object o)
        {
            _mediaManager.ArchiveMedia(_getSelections().Where(m => m is IServerMedia).Cast<IServerMedia>() , true);
        }

        private void _copySelectedToArchive(object o)
        {
            _mediaManager.ArchiveMedia(_getSelections().Where(m => m is IServerMedia).Cast<IServerMedia>(), false);
        }

        private bool _filter(object item)
        {
            if (_mediaDirectory is IArchiveDirectory
                || (_mediaDirectory is IIngestDirectory && ((IIngestDirectory)_mediaDirectory).IsWAN))
                return true;
            var m = item as MediaViewViewmodel;
            string mediaName = m.MediaName == null ? string.Empty:  m.MediaName.ToLower();
            return (!(_mediaDirectory is IServerDirectory || _mediaDirectory is IArchiveDirectory) || _mediaCategory as TMediaCategory? == null || m.MediaCategory == (TMediaCategory)_mediaCategory)
               && (_searchTextSplit.All(s => mediaName.Contains(s)))
               && (_mediaType as TMediaType? == null || m.Media.MediaType == (TMediaType)_mediaType);
        }

        readonly IEnumerable<object> _mediaCategories = (new List<object>(){resources._all_}).Concat(Enum.GetValues(typeof(TMediaCategory)).Cast<object>());
        public IEnumerable<object> MediaCategories { get { return _mediaCategories; } }

        private object _mediaCategory;
        public object MediaCategory
        {
            get { return _mediaCategory; }
            set
            {
                if (SetField(ref _mediaCategory, value, "MediaCategory"))
                {
                    NotifyPropertyChanged("IsDisplayMediaCategory");
                    _search(null);
                }
            }
        }

        readonly IEnumerable<object> _mediaTypes = (new List<object>() { resources._all_ }).Concat(Enum.GetValues(typeof(TMediaType)).Cast<object>());
        public IEnumerable<object> MediaTypes { get { return _mediaTypes; } }

        private object _mediaType = TMediaType.Movie;
        public object MediaType
        {
            get { return _mediaType; }
            set
            {
                if (SetField(ref _mediaType, value, "MediaType"))
                    _search(null);
            }
        }

        readonly List<IMediaDirectory> _mediaDirectories;
        public List<IMediaDirectory> MediaDirectories { get { return _mediaDirectories; } }

        private IMediaDirectory _mediaDirectory;

        public IMediaDirectory MediaDirectory
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
                        if (value is IArchiveDirectory)
                            if (!string.IsNullOrEmpty((value as IArchiveDirectory).SearchString))
                                SearchText = (value as IArchiveDirectory).SearchString;
                    }
                    PreviewDisplay = _previewViewModel != null 
                        && value != null 
                        && (!(value is IIngestDirectory) || (value as IIngestDirectory).AccessType == TDirectoryAccessType.Direct);
                    if (_previewViewModel != null)
                        _previewViewModel.IsSegmentsVisible = value is IServerDirectory || value is IArchiveDirectory;
                    _reloadFiles();
                    SelectedMedia = null;
                    NotifyPropertyChanged("MediaDirectory");
                    NotifyPropertyChanged("DisplayDirectoryInfo");
                    NotifyPropertyChanged("CommandRefresh");
                    NotifyPropertyChanged("IsDisplayFolder");
                    NotifyPropertyChanged("IsDisplayIsArchived");
                    NotifyPropertyChanged("IsDisplayMediaCategory");
                    NotifyPropertyChanged("IsDisplayIngestState");
                    _notifyDirectoryPropertiesChanged();
                }
            }
        }

        public bool IsDisplayFolder
        {
            get
            {
                return _mediaDirectory is IArchiveDirectory || (_mediaDirectory is IIngestDirectory && ((IIngestDirectory)_mediaDirectory).IsRecursive);
            }
        }

        public bool IsDisplayMediaCategory
        {
            get
            {
                return (_mediaDirectory is IServerDirectory || _mediaDirectory is IArchiveDirectory) && 
                    !(_mediaCategory is TMediaCategory);
            }
        }

        public bool IsDisplayIsArchived { get { return _mediaDirectory is IServerDirectory; } }
        public bool IsDisplayIngestState { get { return _mediaDirectory is IIngestDirectory || _mediaDirectory is IArchiveDirectory; } }

        private void MediaDirectoryPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SearchString")
            {
                if (sender is IArchiveDirectory)
                    SearchText = (sender as IArchiveDirectory).SearchString;
            }
            if (e.PropertyName == "IsInitialized" && (sender as IMediaDirectory).IsInitialized)
                Application.Current.Dispatcher.BeginInvoke((Action)delegate() {_reloadFiles();});
            if (e.PropertyName == "VolumeFreeSize")
                _notifyDirectoryPropertiesChanged();
        }

        private void _reloadFiles()
        {
            UiServices.SetBusyState();
            if (_mediaItems != null)
            {
                foreach (var m in _mediaItems)
                    m.Dispose();
            }
            _mediaItems = new ObservableCollection<MediaViewViewmodel>();
            IEnumerable<MediaViewViewmodel> itemsToLoad;
            if (_mediaDirectory is IServerDirectory)
                itemsToLoad = _mediaDirectory.GetFiles().Where(f => (f.MediaType == TMediaType.Movie || f.MediaType == TMediaType.Still)).Select(f => new MediaViewViewmodel(f, _mediaManager));
            else
                itemsToLoad = _mediaDirectory.GetFiles().Select(f => new MediaViewViewmodel(f, _mediaManager));
            foreach (MediaViewViewmodel mvm in itemsToLoad)
                _mediaItems.Add(mvm);
            _mediaView = CollectionViewSource.GetDefaultView(_mediaItems);
            _mediaView.SortDescriptions.Add(new SortDescription("MediaName", ListSortDirection.Ascending));
            if (!(_mediaDirectory is IArchiveDirectory))
                _mediaView.Filter = new Predicate<object>(_filter);
            var ingestdir = _mediaDirectory as IIngestDirectory;
            if (ingestdir != null && ingestdir.IsXDCAM && !ingestdir.IsWAN)
                ThreadPool.QueueUserWorkItem(o => _refreshMediaDirectory(ingestdir));
            NotifyPropertyChanged("MediaItems");
        }


        private void MediaAdded(object source, MediaEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)delegate()
            {
                IMedia media = e.Media;
                if (!(MediaDirectory is IServerDirectory) || (media.MediaType == TMediaType.Movie || media.MediaType == TMediaType.Still))
                {
                    _mediaItems.Add(new MediaViewViewmodel(media, _mediaManager));
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
                MediaViewViewmodel vm = _mediaItems.FirstOrDefault(v => v.Media == e.Media);
                if (vm != null)
                {
                    _mediaItems.Remove(vm);
                    vm.Dispose();
                }
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

        public bool DisplayDirectoryInfo { get { return _mediaDirectory is IServerDirectory || _mediaDirectory is IArchiveDirectory || (_mediaDirectory is IIngestDirectory &&  ((IIngestDirectory)_mediaDirectory).AccessType == TDirectoryAccessType.Direct); } }
        public bool DirectoryFreeOver20Percent { get { return (DirectoryFreePercentage >= 20); } }
        public float DirectoryTotalSpace { get { return _mediaDirectory == null ? 0F : _mediaDirectory.VolumeTotalSize / (1073741824F); } }
        public float DirectoryFreeSpace { get { return _mediaDirectory == null ? 0F : _mediaDirectory.VolumeFreeSize / (1073741824F); } }
        public float DirectoryFreePercentage
        {
            get
            {
                long totalSize = _mediaDirectory == null ? 0 : _mediaDirectory.VolumeTotalSize;
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
