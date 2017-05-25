using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows;
using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using System.Threading;
using TAS.Client.Common;
using resources = TAS.Client.Common.Properties.Resources;
using TAS.Server.Common;
using System.IO;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using TAS.Server.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    public class MediaManagerViewmodel : ViewmodelBase
    {
        private const int MinSearchLength = 3;
        private readonly IMediaManager _mediaManager;
        private ICollectionView _mediaView;
        private readonly IPreview _preview;

        public ICommand CommandSearch { get; } 
        public ICommand CommandClearFilters { get; }
        public ICommand CommandDeleteSelected { get; }
        public ICommand CommandIngestSelectedToServer { get; }
        public ICommand CommandMoveSelectedToArchive { get; }
        public ICommand CommandCopySelectedToArchive { get; }
        public ICommand CommandSweepStaleMedia { get; }
        public ICommand CommandGetLoudness { get; }
        public ICommand CommandExport { get; }
        public ICommand CommandRefresh { get; }
        public ICommand CommandSyncPriToSec { get; }
        public ICommand CommandCloneAnimation { get; }

        public MediaManagerViewmodel(IMediaManager mediaManager, IPreview preview)
        {
            _mediaManager = mediaManager;
            _preview = preview;
            if (preview != null)
                PreviewViewmodel = new PreviewViewmodel(preview);

            MediaDirectories = new List<MediaDirectoryViewmodel>();
            MediaDirectories.AddRange(mediaManager.IngestDirectories.Where(d => d.ContainsImport()).Select(d => new MediaDirectoryViewmodel(d, true)));
            IArchiveDirectory archiveDirectory = mediaManager.ArchiveDirectory;
            if (archiveDirectory != null)
                MediaDirectories.Insert(0, new MediaDirectoryViewmodel(archiveDirectory));

            IAnimationDirectory animationDirectoryPRI = mediaManager.AnimationDirectoryPRI;
            if (animationDirectoryPRI != null)
                MediaDirectories.Insert(0, new MediaDirectoryViewmodel(animationDirectoryPRI));
            IAnimationDirectory animationDirectorySEC = mediaManager.AnimationDirectorySEC;
            if (animationDirectorySEC != null)
                MediaDirectories.Insert(0, new MediaDirectoryViewmodel(animationDirectorySEC));

            IServerDirectory serverDirectoryPRI = mediaManager.MediaDirectoryPRI;
            MediaDirectoryViewmodel serverDirectoryPRIVm = new MediaDirectoryViewmodel(serverDirectoryPRI);
            if (serverDirectoryPRI != null)
                MediaDirectories.Insert(0, serverDirectoryPRIVm);
            IServerDirectory serverDirectorySEC = mediaManager.MediaDirectorySEC;
            if (serverDirectorySEC != null && serverDirectorySEC != serverDirectoryPRI)
                MediaDirectories.Insert(1, new MediaDirectoryViewmodel(serverDirectorySEC));

            _mediaCategory = MediaCategories.FirstOrDefault();
            SelectedDirectory = serverDirectoryPRIVm;
            if (mediaManager.FileManager != null)
                FileManagerViewmodel = new FileManagerViewmodel(mediaManager.FileManager);
            RecordersViewmodel = new RecordersViewmodel(mediaManager.Recorders);
            RecordersViewmodel.PropertyChanged += _recordersViewmodel_PropertyChanged;
            _previewDisplay = true;
            ComposePlugins();
            VideoPreview?.SetSource(RecordersViewmodel.Channel?.PreviewUrl);

            CommandSearch = new UICommand { ExecuteDelegate = _search, CanExecuteDelegate = _canSearch };
            CommandClearFilters = new UICommand { ExecuteDelegate = _clearFilters, CanExecuteDelegate = _canClearFilters };
            CommandDeleteSelected = new UICommand { ExecuteDelegate = _deleteSelected, CanExecuteDelegate = _isSomethingSelected };
            CommandIngestSelectedToServer = new UICommand { ExecuteDelegate = _ingestSelectedToServer, CanExecuteDelegate = _canIngestSelectedToServer };
            CommandMoveSelectedToArchive = new UICommand { ExecuteDelegate = _moveSelectedToArchive, CanExecuteDelegate = o => _selectedDirectory != null && _selectedDirectory.IsServerDirectory && _isSomethingSelected(o) };
            CommandCopySelectedToArchive = new UICommand { ExecuteDelegate = _copySelectedToArchive, CanExecuteDelegate = o => _selectedDirectory != null && _selectedDirectory.IsServerDirectory && _isSomethingSelected(o) };
            CommandSweepStaleMedia = new UICommand { ExecuteDelegate = _sweepStaleMedia };
            CommandGetLoudness = new UICommand { ExecuteDelegate = _getLoudness, CanExecuteDelegate = _isSomethingSelected };
            CommandExport = new UICommand { ExecuteDelegate = _export, CanExecuteDelegate = _canExport };
            CommandRefresh = new UICommand { ExecuteDelegate = ob => _refreshMediaDirectory(_selectedDirectory?.Directory), CanExecuteDelegate = _canRefresh };
            CommandSyncPriToSec = new UICommand { ExecuteDelegate = _syncSecToPri, CanExecuteDelegate = o => _selectedDirectory.IsServerDirectory };
            CommandCloneAnimation = new UICommand { ExecuteDelegate = _cloneAnimation, CanExecuteDelegate = _canCloneAnimation };

        }

        private bool _canRefresh(object obj)
        {
            var directory = _selectedDirectory?.Directory as IIngestDirectory;
            return directory != null && (!directory.IsWAN || _searchText.Length >= MinSearchLength);
        }

        private void _recordersViewmodel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(RecordersViewmodel.RecordingMedia))
            {
                var media = ((RecordersViewmodel)sender).RecordingMedia;
                if (media != null)
                {
                    EditMedia = new MediaEditViewmodel(media, _mediaManager, PreviewViewmodel, true);
                    if (PreviewViewmodel != null)
                        PreviewViewmodel.Media = media;
                }
            }
            if (e.PropertyName == nameof(RecordersViewmodel.Channel))
            {
                VideoPreview?.SetSource(((RecordersViewmodel)sender).Channel?.PreviewUrl);
            }
        }

        public PreviewViewmodel PreviewViewmodel { get; }

#pragma warning disable CS0649 
        [Import(AllowDefault = true)]
        public Common.Plugin.IVideoPreview VideoPreview { get; private set; }
#pragma warning restore

        public FileManagerViewmodel FileManagerViewmodel { get; }

        public RecordersViewmodel RecordersViewmodel { get; }

        bool _previewDisplay;
        public bool PreviewDisplay { get { return _previewDisplay; } set { SetField(ref _previewDisplay, value); } }

        private MediaViewViewmodel _selectedMedia;
        public MediaViewViewmodel SelectedMedia
        {
            get { return _selectedMedia; }
            set
            {
                if (!_checkEditMediaSaved())
                {
                    NotifyPropertyChanged(nameof(SelectedMedia));
                    return;
                }
                var oldSelectedMedia = _selectedMedia;
                if (SetField(ref _selectedMedia, value))
                {
                    if (oldSelectedMedia != null)
                        oldSelectedMedia.SelectedSegment = null;
                    IMedia media = value == null ? null : value.Media;
                    if (media is IIngestMedia
                        && !media.IsVerified)
                        media.ReVerify();
                    if (PreviewViewmodel != null)
                        PreviewViewmodel.Media = media;
                    EditMedia = _selectedMedia == null ? null : new MediaEditViewmodel(_selectedMedia.Media, _mediaManager, PreviewViewmodel, true);
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
                if (SetField(ref _editMedia, value) && oldEditMedia != null)
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
                    InvalidateRequerySuggested();
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
            return _selectedDirectory != null && (_selectedDirectory.IsIngestDirectory || _selectedDirectory.IsArchiveDirectory) && _isSomethingSelected(o);
        }

        bool _canExport(object o)
        {
            return _selectedMediaList != null && _selectedMediaList.Cast<MediaViewViewmodel>().Any(m => m.Media.MediaType == TMediaType.Movie);
        }

        private bool _canClearFilters(object obj)
        {
            return IsMediaCategoryVisible && _mediaCategory != MediaCategories.FirstOrDefault()
                || _mediaType != MediaTypes.FirstOrDefault()
                || !string.IsNullOrWhiteSpace(_searchText);
        }

        private void _clearFilters(object obj)
        {
            _mediaCategory = MediaCategories.FirstOrDefault();
            _mediaType = MediaTypes.FirstOrDefault();
            NotifyPropertyChanged(nameof(MediaCategory));
            NotifyPropertyChanged(nameof(MediaType));
            SearchText = string.Empty;
            if (_canSearch(obj))
                _search(obj);
        }

        private void _cloneAnimation(object obj)
        {
            var dir = _selectedDirectory?.Directory as IAnimationDirectory;
            var media = _selectedMedia?.Media as IAnimatedMedia;
            if (media != null)
                dir?.CloneMedia(media, Guid.NewGuid());
        }

        private bool _canCloneAnimation(object obj)
        {
            return _selectedMedia?.Media is IAnimatedMedia;
        }

        private void _refreshMediaDirectory(IMediaDirectory directory)
        {
            if (directory != null)
                ThreadPool.QueueUserWorkItem(o =>
               {
                   try
                   {
                       directory.Refresh();
                   }
                   catch (Exception e)
                   {
                       if (directory == SelectedDirectory.Directory)
                           Application.Current.Dispatcher.BeginInvoke((Action)delegate
                           {
                               MessageBox.Show(string.Format(resources._message_DirectoryRefreshFailed, e.Message), resources._caption_Error, MessageBoxButton.OK, MessageBoxImage.Hand);
                           });
                   }
               });
        }

        private void _syncSecToPri(object o)
        {
            if (_selectedDirectory?.Directory is IServerDirectory)
                        _mediaManager.SynchronizeMediaSecToPri(true);
        }

        private void _export(object obj)
        {
            var selections = _getSelections().Select(m => new MediaExportDescription(m, new List<IMedia>(), m.TcPlay, m.DurationPlay, m.AudioVolume));
            using (new ExportViewmodel(_mediaManager, selections))
            { }
        }

        private void _sweepStaleMedia(object o)
        {
            SelectedDirectory.SweepStaleMedia();
        }

        private void _getLoudness(object o)
        {
            _mediaManager.MeasureLoudness(_getSelections());
        }

        private void _ingestSelectionToDir(IServerDirectory directory)
        {
            IMediaDirectory currentDir = _selectedDirectory?.Directory;
            if (currentDir is IIngestDirectory)
            {
                List<IConvertOperation> ingestList = new List<IConvertOperation>();
                var selectedMedia = _getSelections();
                ThreadPool.QueueUserWorkItem(o =>
                {
                    selectedMedia.ForEach(m =>
                    {
                        if (!m.IsVerified)
                            m.Verify();
                    });
                });
                foreach (IMedia sourceMedia in selectedMedia)
                {
                    string destFileName = FileUtils.GetUniqueFileName(directory.Folder, $"{Path.GetFileNameWithoutExtension(sourceMedia.FileName)}{FileUtils.DefaultFileExtension(sourceMedia.MediaType)}");
                    IPersistentMediaProperties destMediaProperties = new PersistentMediaProxy {
                        FileName = destFileName,
                        MediaName = FileUtils.GetFileNameWithoutExtension(sourceMedia.FileName, sourceMedia.MediaType),
                        MediaType = sourceMedia.MediaType == TMediaType.Unknown ? TMediaType.Movie : sourceMedia.MediaType,
                        Duration = sourceMedia.Duration,
                        DurationPlay = sourceMedia.DurationPlay,
                        MediaGuid = sourceMedia.MediaGuid,
                        MediaCategory = sourceMedia.MediaCategory
                    };
                        ingestList.Add(
                            FileManagerViewmodel.CreateConvertOperation(
                            sourceMedia,
                            destMediaProperties,
                            directory,
                            _mediaManager.VideoFormat,
                            (sourceMedia.Directory is IIngestDirectory) ? ((IIngestDirectory)sourceMedia.Directory).AudioVolume : 0,
                            (sourceMedia.Directory is IIngestDirectory) ? ((IIngestDirectory)sourceMedia.Directory).SourceFieldOrder : TFieldOrder.Unknown,
                            (sourceMedia.Directory is IIngestDirectory) ? ((IIngestDirectory)sourceMedia.Directory).AspectConversion : TAspectConversion.NoConversion,
                            (sourceMedia.Directory is IIngestDirectory) ? ((IIngestDirectory)sourceMedia.Directory).MediaLoudnessCheckAfterIngest : false
                            ));
                }
                if (ingestList.Count != 0)
                {
                    using (IngestEditViewmodel ievm = new IngestEditViewmodel(ingestList, _preview, _mediaManager))
                    {
                        if (ievm.ShowDialog() == true)
                        {
                            foreach (var operationVm in ievm.OperationList)
                                _mediaManager.FileManager.Queue(operationVm.FileOperation, false);
                        }
                    }
                }
            }
        }

        private bool _checkEditMediaSaved()
        {
            if (EditMedia != null && EditMedia.IsModified)
                switch (MessageBox.Show(String.Format(resources._query_SaveChangedData, EditMedia), resources._caption_Confirmation, MessageBoxButton.YesNoCancel))
                {
                    case MessageBoxResult.Cancel:
                        return false;
                    case MessageBoxResult.Yes:
                        EditMedia.ModelUpdate();
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
                if (_selectedDirectory.IsIngestDirectory)
                    _ingestSelectionToDir(dir);
                else
                    _mediaManager.CopyMediaToPlayout(_getSelections(), true);
            }
        }

        private bool _canSearch(object o)
        {
            var dir = _selectedDirectory?.Directory;
            return dir is IServerDirectory
                || dir is IAnimationDirectory
                || dir is IArchiveDirectory
                || (dir is IIngestDirectory && (!((IIngestDirectory)dir).IsWAN || _searchText.Length >= MinSearchLength));
        }

        private void _search(object o)
        {
            var dir = _selectedDirectory?.Directory;
            if (dir is IArchiveDirectory)
            {
                (dir as IArchiveDirectory).SearchMediaCategory = _mediaCategory as TMediaCategory?;
                (dir as IArchiveDirectory).SearchString = _searchText;
                (dir as IArchiveDirectory).Search();
            }
            else
            if (dir is IIngestDirectory && ((IIngestDirectory)dir).IsWAN)
            {
                if (_canSearch(o))
                    ((IIngestDirectory)dir).Filter = _searchText;
            }
            else
                _mediaView?.Refresh();
            NotifyPropertyChanged(nameof(ItemsCount));
        }

        private string[] _searchTextSplit = new string[0];
        private string _searchText = string.Empty;
        public string SearchText
        {
            get { return _searchText; }
            set
            {
                if (SetField(ref _searchText, value))
                    _searchTextSplit = value.ToLower().Split(' ');
            }
        }

        private void _deleteSelected(object o)
        {
            List<IMedia> selection = _getSelections();
            if (MessageBox.Show(string.Format(resources._query_DeleteSelectedFiles, selection.AsString(Environment.NewLine)), resources._caption_Confirmation, MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                var reasons = _mediaManager.DeleteMedia(selection, false).Where(r => r.Reason != MediaDeleteDenyReason.MediaDeleteDenyReasonEnum.NoDeny);
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
                    {
                        if (MessageBox.Show(String.Join(Environment.NewLine, resources._message_MediaDeleteNotAllowed, reasonMsg.ToString(), Environment.NewLine, resources._message_DeleteAnyway), resources._caption_Error, MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
                            _mediaManager.DeleteMedia(reasons.Select(r => r.Media), true);
                    }
                }
            }
        }

        private void _moveSelectedToArchive(object o)
        {
            _mediaManager.ArchiveMedia(_getSelections().Where(m => m is IServerMedia).Cast<IServerMedia>().ToArray(), true);
        }

        private void _copySelectedToArchive(object o)
        {
            _mediaManager.ArchiveMedia(_getSelections().Where(m => m is IServerMedia).Cast<IServerMedia>().ToArray(), false);
        }

        private bool _filter(object item)
        {
            var dir = _selectedDirectory?.Directory;
            if (dir is IArchiveDirectory
                || (dir is IIngestDirectory && ((IIngestDirectory)dir).IsWAN))
                return true;
            var m = item as MediaViewViewmodel;
            string mediaName = m?.MediaName?.ToLower() ?? string.Empty;
            return (!(dir is IServerDirectory) || !(_mediaCategory is TMediaCategory?) || m?.MediaCategory == (TMediaCategory)_mediaCategory)
               && _searchTextSplit.All(s => mediaName.Contains(s))
               && (!(_mediaType is TMediaType?) || m?.Media.MediaType == (TMediaType)_mediaType);
        }

        public IEnumerable<object> MediaCategories => new List<object> { resources._all_ }.Concat(Enum.GetValues(typeof(TMediaCategory)).Cast<object>());

        private object _mediaCategory;
        public object MediaCategory
        {
            get { return _mediaCategory; }
            set
            {
                if (SetField(ref _mediaCategory, value))
                {
                    NotifyPropertyChanged(nameof(IsDisplayMediaCategory));
                    _search(null);
                }
            }
        }

        public IEnumerable<object> MediaTypes => new List<object> { resources._all_ }.Concat(Enum.GetValues(typeof(TMediaType)).Cast<object>());

        private object _mediaType = resources._all_;
        public object MediaType
        {
            get { return _mediaType; }
            set
            {
                if (SetField(ref _mediaType, value))
                {
                    NotifyPropertyChanged(nameof(IsMediaCategoryVisible));
                    _search(null);
                }
            }
        }

        public List<MediaDirectoryViewmodel> MediaDirectories { get; }

        private MediaDirectoryViewmodel _selectedDirectory;

        public MediaDirectoryViewmodel SelectedDirectory
        {
            get { return _selectedDirectory; }
            set
            {
                if (_checkEditMediaSaved())
                {
                    if (_selectedDirectory != value)
                        _setSelectdDirectory(value);
                }
                else
                    Application.Current.Dispatcher.BeginInvoke((Action)delegate () { NotifyPropertyChanged(nameof(SelectedDirectory)); }); //revert folder display, deferred execution
            }
        }

        private void _setSelectdDirectory(MediaDirectoryViewmodel directory)
        {
            IMediaDirectory dir = _selectedDirectory?.Directory;
            if (dir != null)
            {
                dir.MediaAdded -= _selectedDirectoryMediaAdded;
                dir.MediaRemoved -= _selectedDirectoryMediaRemoved;
                dir.PropertyChanged -= _selectedDirectoryPropertyChanged;
            }
            dir = directory?.Directory;
            _selectedDirectory = directory;
            if (dir != null)
            {
                dir.MediaAdded += _selectedDirectoryMediaAdded;
                dir.MediaRemoved += _selectedDirectoryMediaRemoved;
                dir.PropertyChanged += _selectedDirectoryPropertyChanged;
                if (dir is IArchiveDirectory)
                    if (!string.IsNullOrEmpty((dir as IArchiveDirectory).SearchString))
                        SearchText = (dir as IArchiveDirectory).SearchString;
            }
            PreviewDisplay = PreviewViewmodel != null
                && dir != null
                && (!(dir is IIngestDirectory) || (dir as IIngestDirectory).AccessType == TDirectoryAccessType.Direct);
            if (PreviewViewmodel != null)
                PreviewViewmodel.IsSegmentsVisible = dir is IServerDirectory || dir is IArchiveDirectory;
            _reloadFiles(directory);
            SelectedMedia = null;
            NotifyPropertyChanged(nameof(SelectedDirectory));
            NotifyPropertyChanged(nameof(DisplayDirectoryInfo));
            NotifyPropertyChanged(nameof(IsDisplayFolder));
            NotifyPropertyChanged(nameof(IsServerDirectory));
            NotifyPropertyChanged(nameof(IsMediaCategoryVisible));
            NotifyPropertyChanged(nameof(IsIngestOrArchiveDirectory));
            NotifyPropertyChanged(nameof(IsAnimationDirectory));
            NotifyPropertyChanged(nameof(IsDisplayClipNr));
            InvalidateRequerySuggested();
            _notifyDirectoryPropertiesChanged();
        }

        public bool IsDisplayFolder
        {
            get
            {
                return _selectedDirectory != null && (_selectedDirectory.IsArchiveDirectory || _selectedDirectory.IsRecursive);
            }
        }

        public bool IsDisplayClipNr => _selectedDirectory != null && _selectedDirectory.IsXdcam;

        public bool IsDisplayMediaCategory => _selectedDirectory != null && _selectedDirectory.IsPersistentDirectory && !(_mediaCategory is TMediaCategory);


        public bool IsMediaCategoryVisible => _selectedDirectory != null && _selectedDirectory.IsPersistentDirectory && (!(_mediaType is TMediaType) || Equals(_mediaType, TMediaType.Movie));
        public bool IsServerDirectory => _selectedDirectory != null && _selectedDirectory.IsServerDirectory;
        public bool IsIngestOrArchiveDirectory => _selectedDirectory != null && (_selectedDirectory.IsArchiveDirectory || _selectedDirectory.IsIngestDirectory);
        public bool IsAnimationDirectory => _selectedDirectory != null && _selectedDirectory.IsAnimationDirectory;
        public bool IsMediaExportVisible { get { return MediaDirectories.Any(d => d.IsExport); } }

        private void _selectedDirectoryPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IArchiveDirectory.SearchString))
            {
                if (sender is IArchiveDirectory)
                    SearchText = (sender as IArchiveDirectory).SearchString;
            }
            if (e.PropertyName == nameof(IMediaDirectory.IsInitialized))
            {
                Application.Current.Dispatcher.BeginInvoke((Action)delegate () { _reloadFiles(_selectedDirectory); });
                _notifyDirectoryPropertiesChanged();
            }
            if (e.PropertyName == nameof(IMediaDirectory.VolumeFreeSize))
                _notifyDirectoryPropertiesChanged();
        }

        private void _reloadFiles(MediaDirectoryViewmodel directory)
        {
            if (directory?.IsInitialized == true && (!directory.IsIngestDirectory || directory.IsImport))
            {
                UiServices.SetBusyState();
                if (_mediaItems != null)
                    foreach (var m in _mediaItems)
                        m.Dispose();
                MediaItems = new ObservableCollection<MediaViewViewmodel>(directory.Directory.GetFiles().Select(f => new MediaViewViewmodel(f)));
                _mediaView = CollectionViewSource.GetDefaultView(_mediaItems);
                if (!directory.IsXdcam)
                    _mediaView.SortDescriptions.Add(new SortDescription(nameof(MediaViewViewmodel.MediaName), ListSortDirection.Ascending));
                if (!directory.IsArchiveDirectory)
                    _mediaView.Filter = _filter;
                if (directory.IsXdcam && !directory.IsWan)
                    ThreadPool.QueueUserWorkItem(o => _refreshMediaDirectory(directory.Directory));
            }
            else
                MediaItems = null;
        }


        private void _selectedDirectoryMediaAdded(object source, MediaEventArgs e)
        {
            var dir = source as IMediaDirectory;
            if (dir != null && dir.IsInitialized)
                Application.Current.Dispatcher.BeginInvoke((Action)delegate ()
                {
                    IMedia media = e.Media;
                    if ( _mediaItems != null)
                    {
                        _mediaItems.Add(new MediaViewViewmodel(media));
                        _notifyDirectoryPropertiesChanged();
                    }
                }
                    , null);
        }

        private void _selectedDirectoryMediaRemoved(object source, MediaEventArgs e)
        {
            var dir = source as IMediaDirectory;
            if (dir != null && dir.IsInitialized)
                Application.Current.Dispatcher.BeginInvoke((Action)delegate ()
                {
                    if (_mediaItems != null)
                    {
                        MediaViewViewmodel vm = _mediaItems.FirstOrDefault(v => v.Media == e.Media);
                        if (vm != null)
                        {
                            if (SelectedMedia == vm) 
                                SelectedMedia = null;
                            _mediaItems.Remove(vm);
                            vm.Dispose();
                        }
                        _notifyDirectoryPropertiesChanged();
                    }
                }
                , null);
        }

        private void _notifyDirectoryPropertiesChanged()
        {
            NotifyPropertyChanged(nameof(IsMediaDirectoryOk));
            NotifyPropertyChanged(nameof(DirectoryFreePercentage));
            NotifyPropertyChanged(nameof(DirectoryTotalSpace));
            NotifyPropertyChanged(nameof(DirectoryFreeSpace));
            NotifyPropertyChanged(nameof(ItemsCount));
        }

        public bool DisplayDirectoryInfo
        {
            get
            {
                return _selectedDirectory != null
                       && (_selectedDirectory.IsServerDirectory || _selectedDirectory.IsArchiveDirectory || (_selectedDirectory.IsIngestDirectory && (_selectedDirectory.AccessType == TDirectoryAccessType.Direct || _selectedDirectory.IsXdcam)));
            }
        }
        public bool IsMediaDirectoryOk => _selectedDirectory?.IsOK == true;
        public float DirectoryTotalSpace => _selectedDirectory?.VolumeTotalSize / 1073741824F ?? 0F;
        public float DirectoryFreeSpace => _selectedDirectory?.VolumeFreeSize / 1073741824F ?? 0F;

        public float DirectoryFreePercentage
        {
            get
            {
                return _selectedDirectory == null ? 0 : _selectedDirectory.DirectoryFreePercentage;
            }
        }

        public int ItemsCount => _mediaItems == null ? 0 : _mediaItems.Count(_filter);

        protected override void OnDispose()
        {
            SelectedDirectory = null;
            if (RecordersViewmodel != null)
                RecordersViewmodel.PropertyChanged -= _recordersViewmodel_PropertyChanged;
        }

        private ObservableCollection<MediaViewViewmodel> _mediaItems;
        public ObservableCollection<MediaViewViewmodel> MediaItems
        {
            get { return _mediaItems; }
            private set
            {
                if (SetField(ref _mediaItems, value))
                    SelectedMedia = null;
            }
        }

        private void ComposePlugins()
        {
            try
            {
                var pluginPath = Path.Combine(Directory.GetCurrentDirectory(), "Plugins");
                if (Directory.Exists(pluginPath))
                {
                    DirectoryCatalog catalog = new DirectoryCatalog(pluginPath);
                    var container = new CompositionContainer(catalog);
                    container.SatisfyImportsOnce(this);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        public override string ToString()
        {
            return resources._media;
        }
    }
}



