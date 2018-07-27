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
using TAS.Client.Common;
using TAS.Common;
using System.IO;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Threading.Tasks;
using TAS.Common.Interfaces;
using resources = TAS.Client.Common.Properties.Resources;

namespace TAS.Client.ViewModels
{
    public class MediaManagerViewmodel : ViewModelBase
    {
        private const int MinSearchLength = 3;
        private readonly IMediaManager _mediaManager;
        private readonly IEngine _engine;
        private ICollectionView _mediaView;
        private readonly IPreview _preview;
        bool _isDisplayPreview;
        private MediaViewViewmodel _selectedMedia;
        private MediaEditViewmodel _editMedia;
        private IList _selectedMediaList;
        private string[] _searchTextSplit = new string[0];
        private string _searchText = string.Empty;
        private object _mediaCategory;
        private object _mediaType = resources._all_;
        private MediaDirectoryViewmodel _selectedDirectory;
        private ObservableCollection<MediaViewViewmodel> _mediaItems;
        private bool _isPropertiesPanelVisible = true;

        public MediaManagerViewmodel(IEngine engine, IPreview preview)
        {
            _mediaManager =  engine.MediaManager;
            _engine = engine;
            _preview = preview;
            if (preview != null)
                PreviewViewmodel = new PreviewViewmodel(engine, preview);

            MediaDirectories = new List<MediaDirectoryViewmodel>();
            MediaDirectories.AddRange(_mediaManager.IngestDirectories.Where(d => d.ContainsImport()).Select(d => new MediaDirectoryViewmodel(d, true)));
            IArchiveDirectory archiveDirectory = _mediaManager.ArchiveDirectory;
            if (archiveDirectory != null)
                MediaDirectories.Insert(0, new MediaDirectoryViewmodel(archiveDirectory));
            IAnimationDirectory animationDirectoryPri = _mediaManager.AnimationDirectoryPRI;
            if (animationDirectoryPri != null)
                MediaDirectories.Insert(0, new MediaDirectoryViewmodel(animationDirectoryPri));
            IAnimationDirectory animationDirectorySec = _mediaManager.AnimationDirectorySEC;
            if (animationDirectorySec != null)
                MediaDirectories.Insert(0, new MediaDirectoryViewmodel(animationDirectorySec));
            IServerDirectory serverDirectoryPri = _mediaManager.MediaDirectoryPRI;
            if (serverDirectoryPri != null)
                MediaDirectories.Insert(0, new MediaDirectoryViewmodel(serverDirectoryPri));
            IServerDirectory serverDirectorySec = _mediaManager.MediaDirectorySEC;
            if (serverDirectorySec != null && serverDirectorySec != serverDirectoryPri)
                MediaDirectories.Insert(1, new MediaDirectoryViewmodel(serverDirectorySec));

            _mediaCategory = MediaCategories.FirstOrDefault();
            SelectedDirectory = MediaDirectories.FirstOrDefault();
            if (_mediaManager.FileManager != null)
                FileManagerViewmodel = new FileManagerViewmodel(_mediaManager.FileManager);
            RecordersViewmodel = new RecordersViewmodel(_engine, _mediaManager.Recorders);
            RecordersViewmodel.PropertyChanged += _recordersViewmodel_PropertyChanged;
            ComposePlugins();
            VideoPreview?.SetSource(RecordersViewmodel.Channel?.PreviewUrl);

            CommandSearch = new UICommand { ExecuteDelegate = _search, CanExecuteDelegate = _canSearch };
            CommandClearFilters = new UICommand { ExecuteDelegate = _clearFilters, CanExecuteDelegate = _canClearFilters };
            CommandDeleteSelected = new UICommand { ExecuteDelegate = _deleteSelected, CanExecuteDelegate = o => _isSomethingSelected() && engine.HaveRight(EngineRight.MediaDelete) };
            CommandIngestSelectedToServer = new UICommand { ExecuteDelegate = _ingestSelectedToServer, CanExecuteDelegate = _canIngestSelectedToServer };
            CommandMoveSelectedToArchive = new UICommand { ExecuteDelegate = _moveSelectedToArchive, CanExecuteDelegate = o => _selectedDirectory != null && _selectedDirectory.IsServerDirectory && _isSomethingSelected() && engine.HaveRight(EngineRight.MediaArchive) && engine.HaveRight(EngineRight.MediaDelete) };
            CommandCopySelectedToArchive = new UICommand { ExecuteDelegate = _copySelectedToArchive, CanExecuteDelegate = o => _selectedDirectory != null && _selectedDirectory.IsServerDirectory && _isSomethingSelected() && engine.HaveRight(EngineRight.MediaArchive) };
            CommandSweepStaleMedia = new UICommand { ExecuteDelegate = _sweepStaleMedia, CanExecuteDelegate = o => CurrentUser.IsAdmin };
            CommandGetLoudness = new UICommand { ExecuteDelegate = _getLoudness, CanExecuteDelegate = o => _isSomethingSelected() && engine.HaveRight(EngineRight.MediaEdit) };
            CommandExport = new UICommand { ExecuteDelegate = _export, CanExecuteDelegate = _canExport };
            CommandRefresh = new UICommand { ExecuteDelegate = ob => _refreshMediaDirectory(_selectedDirectory?.Directory), CanExecuteDelegate = _canRefresh };
            CommandSyncPriToSec = new UICommand { ExecuteDelegate = _syncSecToPri, CanExecuteDelegate = o => _selectedDirectory.IsServerDirectory && CurrentUser.IsAdmin};
            CommandCloneAnimation = new UICommand { ExecuteDelegate = _cloneAnimation, CanExecuteDelegate = _canCloneAnimation };
            CommandTogglePropertiesPanel = new UICommand { ExecuteDelegate = o => IsPropertiesPanelVisible = !IsPropertiesPanelVisible };
        }

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
        public ICommand CommandTogglePropertiesPanel { get;  }


        #region PreviewCommands
        public ICommand CommandPreviewPlay => PreviewViewmodel?.CommandPlay;
        public ICommand CommandPreviewUnload => PreviewViewmodel?.CommandUnload;
        public ICommand CommandPreviewFastForward => PreviewViewmodel?.CommandFastForward;
        public ICommand CommandPreviewBackward => PreviewViewmodel?.CommandBackward;
        public ICommand CommandPreviewFastForwardOneFrame => PreviewViewmodel?.CommandFastForwardOneFrame;
        public ICommand CommandPreviewBackwardOneFrame => PreviewViewmodel?.CommandBackwardOneFrame;
        public ICommand CommandPreviewTrimSource => PreviewViewmodel?.CommandTrimSource;
        #endregion

        public PreviewViewmodel PreviewViewmodel { get; }

#pragma warning disable CS0649 
        [Import(AllowDefault = true)]
        public Common.Plugin.IVideoPreview VideoPreview { get; private set; }
#pragma warning restore

        public FileManagerViewmodel FileManagerViewmodel { get; }

        public RecordersViewmodel RecordersViewmodel { get; }

        public bool IsDisplayPreview {
            get => _isDisplayPreview;
            private set => SetField(ref _isDisplayPreview, value);
        }

        public MediaViewViewmodel SelectedMedia
        {
            get => _selectedMedia;
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
                        PreviewViewmodel.SelectedMedia = media;
                    EditMedia = _selectedMedia == null ? null : new MediaEditViewmodel(_selectedMedia.Media, _mediaManager, true);
                }
            }
        }

        public MediaEditViewmodel EditMedia
        {
            get => _editMedia;
            set
            {
                var oldEditMedia = _editMedia;
                if (SetField(ref _editMedia, value))
                    oldEditMedia?.Dispose();
            }
        }

        public IList SelectedMediaList
        {
            get => _selectedMediaList;
            set
            {
                _selectedMediaList = value;
                if (value != null)
                    InvalidateRequerySuggested();
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetField(ref _searchText, value))
                    _searchTextSplit = value.ToLower().Split(' ');
            }
        }
        
        public IEnumerable<object> MediaCategories => new List<object> { resources._all_ }.Concat(Enum.GetValues(typeof(TMediaCategory)).Cast<object>());
        public object MediaCategory
        {
            get => _mediaCategory;
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
        public object MediaType
        {
            get => _mediaType;
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

        public MediaDirectoryViewmodel SelectedDirectory
        {
            get => _selectedDirectory;
            set
            {
                if (_checkEditMediaSaved())
                {
                    if (_selectedDirectory != value)
                        _setSelectdDirectory(value);
                }
                else
                    Application.Current?.Dispatcher.BeginInvoke((Action)delegate { NotifyPropertyChanged(nameof(SelectedDirectory)); }); //revert folder display, deferred execution
            }
        }

        public bool IsPropertiesPanelVisible
        {
            get => _isPropertiesPanelVisible;
            set => SetField(ref _isPropertiesPanelVisible, value);
        }


        public bool IsDisplayFolder => _selectedDirectory != null && (_selectedDirectory.IsArchiveDirectory || _selectedDirectory.IsRecursive);

        public bool IsDisplayClipNr => _selectedDirectory != null && _selectedDirectory.IsXdcam;

        public bool IsDisplayMediaCategory => _selectedDirectory != null && _selectedDirectory.IsPersistentDirectory && !(_mediaCategory is TMediaCategory);

        public bool IsMediaCategoryVisible => _selectedDirectory != null && _selectedDirectory.IsPersistentDirectory && (!(_mediaType is TMediaType) || Equals(_mediaType, TMediaType.Movie));

        public bool IsServerDirectory => _selectedDirectory != null && _selectedDirectory.IsServerDirectory;

        public bool IsIngestOrArchiveDirectory => _selectedDirectory != null && (_selectedDirectory.IsArchiveDirectory || _selectedDirectory.IsIngestDirectory);

        public bool IsAnimationDirectory => _selectedDirectory != null && _selectedDirectory.IsAnimationDirectory;

        public bool IsMediaExportVisible { get { return MediaDirectories.Any(d => d.IsExport) && _engine.HaveRight(EngineRight.MediaExport); } }

        public bool DisplayDirectoryInfo => _selectedDirectory != null
                                            && (_selectedDirectory.IsServerDirectory || _selectedDirectory.IsArchiveDirectory || (_selectedDirectory.IsIngestDirectory && (_selectedDirectory.AccessType == TDirectoryAccessType.Direct || _selectedDirectory.IsXdcam)));

        public bool IsMediaDirectoryOk => _selectedDirectory?.IsOK == true;

        public float DirectoryTotalSpace => _selectedDirectory?.VolumeTotalSize / 1073741824F ?? 0F;

        public float DirectoryFreeSpace => _selectedDirectory?.VolumeFreeSize / 1073741824F ?? 0F;

        public float DirectoryFreePercentage => _selectedDirectory == null ? 0 : _selectedDirectory.DirectoryFreePercentage;

        public int ItemsCount => _mediaItems?.Count(_filter) ?? 0;

        public ObservableCollection<MediaViewViewmodel> MediaItems
        {
            get => _mediaItems;
            private set
            {
                if (SetField(ref _mediaItems, value))
                    SelectedMedia = null;
            }
        }
        
        public override string ToString()
        {
            return resources._media;
        }

        protected override void OnDispose()
        {
            SelectedDirectory = null;
            if (RecordersViewmodel != null)
                RecordersViewmodel.PropertyChanged -= _recordersViewmodel_PropertyChanged;
        }


        // private methods
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

        private void _deleteSelected(object o)
        {
            var selections = _getSelections();
            if (MessageBox.Show(
                    string.Format(resources._query_DeleteSelectedFiles, selections.AsString(Environment.NewLine)),
                    resources._caption_Confirmation, MessageBoxButton.OKCancel) != MessageBoxResult.OK)
                return;
            var reasons = _mediaManager.DeleteMedia(selections, false)
                .Where(r => r.Result != MediaDeleteResult.MediaDeleteResultEnum.Success).ToArray();
            if (reasons.Length == 0)
                return;
            var reasonMsg = new StringBuilder();
            foreach (var reason in reasons)
            {
                switch (reason.Result)
                {
                    case MediaDeleteResult.MediaDeleteResultEnum.Success:
                        break;
                    case MediaDeleteResult.MediaDeleteResultEnum.InFutureSchedule:
                        reasonMsg.AppendLine().Append(reason.Media.MediaName).Append(": ").AppendFormat(
                            resources._message_MediaDeleteResult_Scheduled,
                            reason.Event == null ? resources._unknown_ : reason.Event.EventName,
                            reason.Event == null
                                ? resources._unknown_
                                : reason.Event.ScheduledTime.ToLocalTime().ToString());
                        break;
                    case MediaDeleteResult.MediaDeleteResultEnum.Protected:
                        reasonMsg.AppendLine().Append(reason.Media.MediaName).Append(": ")
                            .Append(resources._message_MediaDeleteResult_Protected);
                        break;
                    case MediaDeleteResult.MediaDeleteResultEnum.InsufficentRights:
                        reasonMsg.AppendLine().Append(reason.Media.MediaName).Append(": ")
                            .Append(resources._message_MediaDeleteResult_InsufficientRights);
                        break;
                    default:
                        reasonMsg.AppendLine().Append(reason.Media.MediaName).Append(": ")
                            .Append(resources._message_MediaDeleteResult_Unknown);
                        break;
                }
            }
            if (reasonMsg.Length <= 0)
                return;
            if (MessageBox.Show(
                    String.Join(Environment.NewLine, resources._message_MediaDeleteResult_NotAllowed,
                        reasonMsg.ToString(), Environment.NewLine, resources._message_DeleteAnyway),
                    resources._caption_Error, MessageBoxButton.YesNo, MessageBoxImage.Error) ==
                MessageBoxResult.Yes)
                _mediaManager.DeleteMedia(reasons.Select(r => r.Media).ToArray(), true);
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
            IsDisplayPreview = PreviewViewmodel != null
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

        private bool _canRefresh(object obj)
        {
            return _selectedDirectory?.Directory is IIngestDirectory directory 
                && (!directory.IsWAN || _searchText.Length >= MinSearchLength);
        }

        private void _recordersViewmodel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(RecordersViewmodel.RecordingMedia))
            {
                var media = ((RecordersViewmodel)sender).RecordingMedia;
                if (media != null)
                {
                    EditMedia = new MediaEditViewmodel(media, _mediaManager, true);
                    if (PreviewViewmodel != null)
                        PreviewViewmodel.SelectedMedia = media;
                }
            }
            if (e.PropertyName == nameof(RecordersViewmodel.Channel))
            {
                VideoPreview?.SetSource(((RecordersViewmodel)sender).Channel?.PreviewUrl);
            }
        }

        private void _selectedDirectoryPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IArchiveDirectory.SearchString))
            {
                if (sender is IArchiveDirectory directory)
                    SearchText = directory.SearchString;
            }
            if (e.PropertyName == nameof(IMediaDirectory.IsInitialized))
            {
                Application.Current?.Dispatcher.BeginInvoke((Action)delegate { _reloadFiles(_selectedDirectory); });
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
                _mediaView = CollectionViewSource.GetDefaultView(MediaItems);
                if (!directory.IsXdcam)
                    _mediaView.SortDescriptions.Add(new SortDescription(nameof(MediaViewViewmodel.LastUpdated), ListSortDirection.Descending));
                if (!directory.IsArchiveDirectory)
                    _mediaView.Filter = _filter;
                if (directory.IsXdcam && !directory.IsWan)
                    Task.Run(() => _refreshMediaDirectory(directory.Directory));
            }
            else
                MediaItems = null;
        }
        
        private void _selectedDirectoryMediaAdded(object source, MediaEventArgs e)
        {
            if (source is IMediaDirectory dir && dir.IsInitialized)
                Application.Current?.Dispatcher.BeginInvoke((Action)delegate
                    {
                        var media = e.Media;
                        _mediaItems?.Add(new MediaViewViewmodel(media));
                        _notifyDirectoryPropertiesChanged();
                    }
                    , null);
        }

        private void _selectedDirectoryMediaRemoved(object source, MediaEventArgs e)
        {
            if (source is IMediaDirectory dir && dir.IsInitialized)
                Application.Current?.Dispatcher.BeginInvoke((Action) delegate
                    {
                        var vm = _mediaItems?.FirstOrDefault(v => v.Media == e.Media);
                        if (vm != null)
                        {
                            if (SelectedMedia == vm)
                                SelectedMedia = null;
                            _mediaItems.Remove(vm);
                            vm.Dispose();
                        }
                        _notifyDirectoryPropertiesChanged();
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

        private List<IMedia> _getSelections()
        {
            return _selectedMediaList?.OfType<MediaViewViewmodel>().Select(m => m.Media).ToList() ?? new List<IMedia>();
        }

        private bool _isSomethingSelected()
        {
            return _selectedMediaList != null && _selectedMediaList.Count > 0;
        }

        private bool _canIngestSelectedToServer(object o)
        {
            return _selectedDirectory != null && _engine.HaveRight(EngineRight.MediaIngest) && (_selectedDirectory.IsIngestDirectory || _selectedDirectory.IsArchiveDirectory) && _isSomethingSelected();
        }

        private bool _canExport(object o)
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
            return _selectedMedia?.Media is IAnimatedMedia && _engine.HaveRight(EngineRight.MediaEdit);
        }

        private void _refreshMediaDirectory(IMediaDirectory directory)
        {
            if (directory != null)
                Task.Run(() =>
                {
                    try
                    {
                        directory.Refresh();
                    }
                    catch (Exception e)
                    {
                        if (directory == SelectedDirectory.Directory)
                            Application.Current?.Dispatcher.BeginInvoke((Action)delegate
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
            using (var vm = new ExportViewmodel(_engine, selections))
            {
                UiServices.ShowDialog<Views.ExportView>(vm);
            }
        }

        private void _sweepStaleMedia(object o)
        {
            SelectedDirectory.SweepStaleMedia();
        }

        private void _getLoudness(object o)
        {
            _mediaManager.MeasureLoudness(_getSelections());
        }

        private void _ingestSelectionToDir()
        {
            IMediaDirectory currentDir = _selectedDirectory?.Directory;
            if (currentDir is IIngestDirectory)
            {
                List<IIngestOperation> ingestList = new List<IIngestOperation>();
                var selectedMedia = _getSelections();
                Task.Run(() =>
                {
                    selectedMedia.ForEach(m =>
                    {
                        if (!m.IsVerified)
                            m.Verify();
                    });
                });
                foreach (var sourceMedia in selectedMedia)
                    if (sourceMedia is IIngestMedia media)
                        ingestList.Add(_mediaManager.FileManager.CreateIngestOperation(media, _mediaManager));
                if (ingestList.Count != 0)
                {
                    using (IngestEditorViewmodel ievm = new IngestEditorViewmodel(ingestList, _preview, _engine))
                    {
                        if (UiServices.ShowDialog<Views.IngestEditorView>(ievm) == true)
                            ievm.ScheduleAll();
                    }
                }
            }
        }

        private bool _checkEditMediaSaved()
        {
            if (EditMedia == null || !EditMedia.IsModified)
                return true;
            switch (MessageBox.Show(String.Format(resources._query_SaveChangedData, EditMedia), resources._caption_Confirmation, MessageBoxButton.YesNoCancel))
            {
                case MessageBoxResult.Cancel:
                    return false;
                case MessageBoxResult.Yes:
                    EditMedia.Save();
                    break;
            }
            return true;
        }

        private void _ingestSelectedToServer(object o)
        {
            if (!_checkEditMediaSaved())
                return;
            if (_selectedDirectory.IsIngestDirectory)
                _ingestSelectionToDir();
            else
                _mediaManager.CopyMediaToPlayout(_getSelections(), true);
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

    }
}



