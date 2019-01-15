using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows;
using System.Collections;
using System.Collections.ObjectModel;
using System.Windows.Input;
using TAS.Client.Common;
using TAS.Common;
using System.Globalization;
using System.Threading.Tasks;
using TAS.Client.Common.Plugin;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.MediaDirectory;
using resources = TAS.Client.Common.Properties.Resources;

namespace TAS.Client.ViewModels
{
    public class MediaManagerViewmodel : ViewModelBase, IUiPluginContext
    {
        private const int MinSearchLength = 3;
        private readonly IMediaManager _mediaManager;
        private readonly IPreview _preview;
        bool _isDisplayPreview;
        private MediaViewViewmodel _selectedMediaVm;
        private MediaEditViewmodel _editMedia;
        private IList _selectedMediaList;
        private string[] _searchTextSplit = new string[0];
        private string _searchText = string.Empty;
        private object _mediaCategory;
        private object _mediaType = resources._all_;
        private MediaDirectoryViewmodel _selectedDirectory;
        private ObservableCollection<MediaViewViewmodel> _mediaItems;
        private bool _isPropertiesPanelVisible = true;
        private ICollectionView _mediaItemsView;
        private IMedia _selectedMedia;

        public MediaManagerViewmodel(IEngine engine, IPreview preview)
        {
            _mediaManager = engine.MediaManager;
            Engine = engine;
            _preview = preview;
            if (preview != null)
                PreviewViewmodel = new PreviewViewmodel(engine, preview);

            MediaDirectories = new List<MediaDirectoryViewmodel>();
            MediaDirectories.AddRange(_mediaManager.IngestDirectories.Where(d => d.ContainsImport()).Select(d => new MediaDirectoryViewmodel(d, d.DirectoryName, true)));
            var archiveDirectory = _mediaManager.ArchiveDirectory;
            if (archiveDirectory != null)
                MediaDirectories.Insert(0, new MediaDirectoryViewmodel(archiveDirectory, resources._archive));
            var animationDirectorySec = _mediaManager.AnimationDirectorySEC;
            var animationDirectoryPri = _mediaManager.AnimationDirectoryPRI;
            if (animationDirectorySec != null && animationDirectorySec != animationDirectoryPri)
                MediaDirectories.Insert(0, new MediaDirectoryViewmodel(animationDirectorySec, resources._animations_Secondary));
            if (animationDirectoryPri != null)
                MediaDirectories.Insert(0, new MediaDirectoryViewmodel(animationDirectoryPri, resources._animations_Primary));
            var serverDirectoryPri = _mediaManager.MediaDirectoryPRI;
            var serverDirectorySec = _mediaManager.MediaDirectorySEC;
            if (serverDirectorySec != null && serverDirectorySec != serverDirectoryPri)
                MediaDirectories.Insert(0, new MediaDirectoryViewmodel(serverDirectorySec, resources._secondary));
            if (serverDirectoryPri != null)
                MediaDirectories.Insert(0, new MediaDirectoryViewmodel(serverDirectoryPri, resources._primary));

            _mediaCategory = MediaCategories.FirstOrDefault();
            SelectedDirectory = MediaDirectories.FirstOrDefault();
            if (_mediaManager.FileManager != null)
                FileManagerViewmodel = new FileManagerViewmodel(_mediaManager.FileManager);
            RecordersViewmodel = new RecordersViewmodel(Engine, _mediaManager.Recorders);
            RecordersViewmodel.PropertyChanged += _recordersViewmodel_PropertyChanged;
            VideoPreview = UiPluginManager.ComposePart<IVideoPreview>(this);
            VideoPreview?.SetSource(RecordersViewmodel.Channel?.PreviewUrl);

            CommandSearch = new UiCommand(_search, _canSearch);
            CommandClearFilters = new UiCommand(_clearFilters, _canClearFilters);
            CommandDeleteSelected = new UiCommand(_deleteSelected, o => _isSomethingSelected() && engine.HaveRight(EngineRight.MediaDelete));
            CommandIngestSelectedToServer = new UiCommand(_ingestSelectedToServer, _canIngestSelectedToServer);
            CommandMoveSelectedToArchive = new UiCommand(_moveSelectedToArchive, o => _selectedDirectory != null && _selectedDirectory.IsServerDirectory && _isSomethingSelected() && engine.HaveRight(EngineRight.MediaArchive) && engine.HaveRight(EngineRight.MediaDelete));
            CommandCopySelectedToArchive = new UiCommand(_copySelectedToArchive, o => _selectedDirectory != null && _selectedDirectory.IsServerDirectory && _isSomethingSelected() && engine.HaveRight(EngineRight.MediaArchive));
            CommandSweepStaleMedia = new UiCommand(_sweepStaleMedia, o => CurrentUser.IsAdmin);
            CommandGetLoudness = new UiCommand(_getLoudness, o => _isSomethingSelected() && engine.HaveRight(EngineRight.MediaEdit));
            CommandExport = new UiCommand(_export, _canExport);
            CommandRefresh = new UiCommand(async ob => await _reloadFiles(), _canRefresh);
            CommandSyncPriToSec = new UiCommand(_syncSecToPri, o => _selectedDirectory.IsServerDirectory && CurrentUser.IsAdmin);
            CommandCloneAnimation = new UiCommand(_cloneAnimation, _canCloneAnimation);
            CommandTogglePropertiesPanel = new UiCommand(o => IsPropertiesPanelVisible = !IsPropertiesPanelVisible);
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
        public ICommand CommandTogglePropertiesPanel { get; }


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

        public IVideoPreview VideoPreview { get; }

        public IEngine Engine { get; }

        public FileManagerViewmodel FileManagerViewmodel { get; }

        public RecordersViewmodel RecordersViewmodel { get; }

        public bool IsDisplayPreview
        {
            get => _isDisplayPreview;
            private set => SetField(ref _isDisplayPreview, value);
        }

        public MediaViewViewmodel SelectedMediaVm
        {
            get => _selectedMediaVm;
            set
            {
                if (!_checkEditMediaSaved())
                {
                    NotifyPropertyChanged(nameof(SelectedMediaVm));
                    return;
                }
                var oldSelectedMedia = _selectedMediaVm;
                if (!SetField(ref _selectedMediaVm, value))
                    return;
                SelectedMedia = value?.Media;
                if (oldSelectedMedia != null)
                {
                    oldSelectedMedia.SelectedSegment = null;
                    oldSelectedMedia.Dispose();
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

        public IMedia SelectedMedia
        {
            get => _selectedMedia;
            private set
            {
                if (!SetField(ref _selectedMedia, value))
                    return;
                if (value is IIngestMedia && !value.IsVerified)
                    Task.Run(() => value.Verify(true));
                EditMedia = value == null ? null : new MediaEditViewmodel(value, _mediaManager, true);
                if (PreviewViewmodel != null)
                    PreviewViewmodel.SelectedMedia = value;
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
                if (!SetField(ref _searchText, value))
                    return;
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
                    var oldSelectedDirectory = SelectedDirectory;
                    if (!SetField(ref _selectedDirectory, value))
                        return;
                    if (oldSelectedDirectory != null)
                    {
                        oldSelectedDirectory.Directory.MediaAdded -= _selectedDirectoryMediaAdded;
                        oldSelectedDirectory.Directory.MediaRemoved -= _selectedDirectoryMediaRemoved;
                        oldSelectedDirectory.Directory.PropertyChanged -= _selectedDirectoryPropertyChanged;
                    }
                    if (value != null)
                    {
                        value.Directory.MediaAdded += _selectedDirectoryMediaAdded;
                        value.Directory.MediaRemoved += _selectedDirectoryMediaRemoved;
                        value.Directory.PropertyChanged += _selectedDirectoryPropertyChanged;
                        Application.Current?.Dispatcher.InvokeAsync(_reloadFiles);
                        IsDisplayPreview = PreviewViewmodel != null
                                           && (!(value.Directory is IIngestDirectory) ||
                                               ((IIngestDirectory)value.Directory).AccessType ==
                                               TDirectoryAccessType.Direct);

                        if (PreviewViewmodel != null)
                            PreviewViewmodel.IsSegmentsVisible =
                                value.Directory is IServerDirectory || value.Directory is IArchiveDirectory;
                    }
                    else
                        IsDisplayPreview = false;
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
                else
                    OnIdle(() =>
                        NotifyPropertyChanged(nameof(SelectedDirectory))); //revert folder display, deferred execution
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

        public bool IsMediaExportVisible { get { return MediaDirectories.Any(d => d.IsExport) && Engine.HaveRight(EngineRight.MediaExport); } }

        public bool DisplayDirectoryInfo => _selectedDirectory != null
                                            && (_selectedDirectory.IsServerDirectory || _selectedDirectory.IsArchiveDirectory || (_selectedDirectory.IsIngestDirectory && (_selectedDirectory.AccessType == TDirectoryAccessType.Direct || _selectedDirectory.IsXdcam)));

        public bool IsMediaDirectoryOk => _selectedDirectory?.IsOK == true;

        public float DirectoryTotalSpace => _selectedDirectory?.VolumeTotalSize / 1073741824F ?? 0F;

        public float DirectoryFreeSpace => _selectedDirectory?.VolumeFreeSize / 1073741824F ?? 0F;

        public float DirectoryFreePercentage => _selectedDirectory?.DirectoryFreePercentage ?? 0;

        public int ItemsCount => _mediaItems?.Count(_filter) ?? 0;

        public ICollectionView MediaItemsView { get => _mediaItemsView; private set => SetField(ref _mediaItemsView, value); }

        public bool IsShowRecorders => RecordersViewmodel.Recorders.Any();


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

        private bool _checkAskMediaDelete(IEnumerable<MediaDeleteResult> results)
        {
            var reasons = results.Where(r => r.Result != MediaDeleteResult.MediaDeleteResultEnum.Success).ToArray();
            if (reasons.Length == 0)
                return false;
            var reasonMsg = new StringBuilder();
            foreach (var reason in reasons)
            {
                switch (reason.Result)
                {
                    case MediaDeleteResult.MediaDeleteResultEnum.Success:
                        break;
                    case MediaDeleteResult.MediaDeleteResultEnum.InSchedule:
                        reasonMsg.AppendLine().Append(reason.Media.MediaName).Append(": ").AppendFormat(
                            resources._message_MediaDeleteResult_Scheduled,
                            reason.Event == null ? resources._unknown_ : reason.Event.EventName,
                            reason.Event == null
                                ? resources._unknown_
                                : reason.Event.ScheduledTime.ToLocalTime().ToString(CultureInfo.CurrentCulture));
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
                return false;
            return MessageBox.Show(
                       string.Join(Environment.NewLine, resources._message_MediaDeleteResult_NotAllowed,
                           reasonMsg.ToString(), Environment.NewLine, resources._message_DeleteAnyway),
                       resources._caption_Error, MessageBoxButton.YesNo, MessageBoxImage.Error) ==
                   MessageBoxResult.Yes;
        }

        private void _deleteSelected(object o)
        {
            var selections = _getSelections();
            if (MessageBox.Show(
                    string.Format(resources._query_DeleteSelectedFiles, selections.AsString(Environment.NewLine)),
                    resources._caption_Confirmation, MessageBoxButton.OKCancel) != MessageBoxResult.OK)
                return;
            var reasons = _mediaManager.MediaDelete(selections, false);
            if (_checkAskMediaDelete(reasons))
                _mediaManager.MediaDelete(reasons.Where(r => r.Result != MediaDeleteResult.MediaDeleteResultEnum.Success).Select(r => r.Media).ToArray(), true);
        }

        private void _moveSelectedToArchive(object o)
        {
            var selections = _getSelections();
            var reasons = _mediaManager.MediaArchive(selections, true, false);
            if (_checkAskMediaDelete(reasons))
                _mediaManager.MediaArchive(reasons.Where(r => r.Result != MediaDeleteResult.MediaDeleteResultEnum.Success).Select(r => r.Media).ToArray(), true, true);
        }

        private void _copySelectedToArchive(object o)
        {
            _mediaManager.MediaArchive(_getSelections(), false, false);
        }

        private bool _filter(object item)
        {
            var dir = _selectedDirectory?.Directory;
            if (dir is IArchiveDirectory
                || (dir is IIngestDirectory && ((IIngestDirectory)dir).IsWAN))
                return true;
            if (!(item is MediaViewViewmodel m))
                return false;
            var mediaName = m.MediaName?.ToLower() ?? string.Empty;
            return (!(dir is IServerDirectory) || !(_mediaCategory is TMediaCategory) || m.MediaCategory == (TMediaCategory)_mediaCategory)
                   && _searchTextSplit.All(s => mediaName.Contains(s))
                   && (!(_mediaType is TMediaType mediaType) || m.Media.MediaType == mediaType);
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
            if (e.PropertyName == nameof(IWatcherDirectory.IsInitialized))
            {
                OnUiThread(async () => await _reloadFiles());
                _notifyDirectoryPropertiesChanged();
            }
            if (e.PropertyName == nameof(IMediaDirectory.VolumeFreeSize))
                _notifyDirectoryPropertiesChanged();
        }

        private async Task _reloadFiles()
        {
            UiServices.SetBusyState();
            var selectedDirectory = SelectedDirectory.Directory;
            _setMediaItems(null);
            await Task.Run(async () =>
            {
                if (selectedDirectory is IArchiveDirectory archiveDirectory)
                {
                    var mediaItems = archiveDirectory.Search(MediaCategory as TMediaCategory?, "");
                    OnUiThread(() => _setMediaItems(mediaItems));
                }
                else if (selectedDirectory is IIngestDirectory ingestDirectory)
                {
                    if (!ingestDirectory.IsWAN)
                    {
                        if (ingestDirectory.Kind == TIngestDirectoryKind.XDCAM)
                        {
                            try
                            {
                                await Task.Run(() => ingestDirectory.Refresh());
                                var mediaItems = ingestDirectory.GetFiles();
                                OnUiThread(() => _setMediaItems(mediaItems));
                            }
                            catch (Exception e)
                            {
                                if (ingestDirectory == SelectedDirectory.Directory)
                                    MessageBox.Show(string.Format(resources._message_DirectoryRefreshFailed, e.Message),
                                        resources._caption_Error, MessageBoxButton.OK, MessageBoxImage.Hand);
                            }
                        }
                        else if (ingestDirectory.Kind == TIngestDirectoryKind.SimpleFolder)
                        {
                            var mediaItems = ingestDirectory.Search(MediaCategory as TMediaCategory?, "");
                            OnUiThread(() => _setMediaItems(mediaItems));
                        }
                        else
                        {
                            var mediaItems = ingestDirectory.GetFiles();
                            OnUiThread(() => _setMediaItems(mediaItems));
                        }
                    }
                    else
                    {
                        var mediaItems = ingestDirectory.GetFiles();
                        OnUiThread(() => _setMediaItems(mediaItems));
                    }
                }
                else if (selectedDirectory is IServerDirectory serverDirectory)
                {
                    var mediaItems = serverDirectory.GetFiles();
                    OnUiThread(() => _setMediaItems(mediaItems));
                }
                else if (selectedDirectory is IAnimationDirectory animationDirectory)
                {
                    var mediaItems = animationDirectory.GetFiles();
                    OnUiThread(() => _setMediaItems(mediaItems));
                }
            });
        }

        private void _setMediaItems(IEnumerable<IMedia> items)
        {
            var newItems = items == null ? null : new ObservableCollection<MediaViewViewmodel>(items.Select(f => new MediaViewViewmodel(f)));
            var oldMediaItems = _mediaItems;
            _mediaItems = newItems;
            if (oldMediaItems != null)
                foreach (var m in oldMediaItems)
                    m.Dispose();
            SelectedMediaVm = null;
            MediaItemsView = newItems == null ? null : CollectionViewSource.GetDefaultView(newItems);
            if (MediaItemsView != null)
            {
                MediaItemsView.Filter = _filter;
                if (!SelectedDirectory.IsXdcam)
                    MediaItemsView.SortDescriptions.Add(new SortDescription(nameof(MediaViewViewmodel.LastUpdated),
                        ListSortDirection.Descending));
            }
            NotifyPropertyChanged(nameof(ItemsCount));
        }

        private void _selectedDirectoryMediaAdded(object source, MediaEventArgs e)
        {
            if (source is IWatcherDirectory dir && dir.IsInitialized)
                OnUiThread(() =>
                {
                    var media = e.Media;
                    _mediaItems?.Add(new MediaViewViewmodel(media));
                    _notifyDirectoryPropertiesChanged();
                });
        }

        private void _selectedDirectoryMediaRemoved(object source, MediaEventArgs e)
        {
            if (source is IWatcherDirectory dir && dir.IsInitialized)
                OnUiThread(() =>
                {
                    var vm = _mediaItems?.FirstOrDefault(v => v.Media == e.Media);
                    if (vm != null)
                    {
                        if (SelectedMediaVm == vm)
                            SelectedMediaVm = null;
                        _mediaItems.Remove(vm);
                        vm.Dispose();
                    }
                    _notifyDirectoryPropertiesChanged();
                });
        }

        private void _notifyDirectoryPropertiesChanged()
        {
            NotifyPropertyChanged(nameof(IsMediaDirectoryOk));
            NotifyPropertyChanged(nameof(DirectoryFreePercentage));
            NotifyPropertyChanged(nameof(DirectoryTotalSpace));
            NotifyPropertyChanged(nameof(DirectoryFreeSpace));
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
            return _selectedDirectory != null && Engine.HaveRight(EngineRight.MediaIngest) && (_selectedDirectory.IsIngestDirectory || _selectedDirectory.IsArchiveDirectory) && _isSomethingSelected();
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
            if (_selectedMediaVm?.Media is IAnimatedMedia media)
                dir?.CloneMedia(media, Guid.NewGuid());
        }

        private bool _canCloneAnimation(object obj)
        {
            return _selectedMediaVm?.Media is IAnimatedMedia && Engine.HaveRight(EngineRight.MediaEdit);
        }


        private async void _syncSecToPri(object o)
        {
            if (_selectedDirectory?.Directory is IServerDirectory)
                await Task.Run(() => _mediaManager.SynchronizeMediaSecToPri(true));
        }

        private void _export(object obj)
        {
            var selections = _getSelections().Select(m => new MediaExportDescription(m, new List<IMedia>(), m.TcPlay, m.DurationPlay, m.AudioVolume));
            using (var vm = new ExportViewmodel(Engine, selections))
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

        private async Task _ingestSelectionToDir()
        {
            var currentDir = _selectedDirectory?.Directory;
            if (!(currentDir is IIngestDirectory))
                return;
            var ingestList = new List<IIngestOperation>();
            var selectedMediaList = _getSelections();
            await Task.Run(() =>
            {
                selectedMediaList.ForEach(m =>
                {
                    if (!m.IsVerified)
                        m.Verify(true);
                });
            });
            foreach (var sourceMedia in selectedMediaList)
                if (sourceMedia is IIngestMedia media)
                    ingestList.Add(await Task.Run(() => _mediaManager.FileManager.CreateIngestOperation(media, _mediaManager)));
            if (ingestList.Count == 0)
                return;
            using (var ievm = new IngestEditorViewmodel(ingestList, _preview, Engine))
            {
                if (UiServices.ShowDialog<Views.IngestEditorView>(ievm) == true)
                    ievm.ScheduleAll();
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

        private async void _ingestSelectedToServer(object o)
        {
            if (!_checkEditMediaSaved())
                return;
            if (_selectedDirectory.IsIngestDirectory)
                await _ingestSelectionToDir();
            else
                await Task.Run(() => _mediaManager.CopyMediaToPlayout(_getSelections()));
        }

        private bool _canSearch(object o)
        {
            var dir = _selectedDirectory?.Directory;
            return dir is IServerDirectory
                   || dir is IAnimationDirectory
                   || dir is IArchiveDirectory
                   || (dir is IIngestDirectory && (!((IIngestDirectory)dir).IsWAN || _searchText.Length >= MinSearchLength));
        }

        private async void _search(object o)
        {
            var dir = _selectedDirectory?.Directory;
            if (dir is IArchiveDirectory archiveDirectory)
                _setMediaItems(await Task.Run(() => archiveDirectory.Search(_mediaCategory as TMediaCategory?, _searchText)));
            else
            if (dir is IIngestDirectory ingestDirectory && (ingestDirectory.IsWAN || ingestDirectory.Kind == TIngestDirectoryKind.SimpleFolder))
                _setMediaItems(await Task.Run(() => ingestDirectory.Search(_mediaCategory as TMediaCategory?, _searchText)));
            else
                MediaItemsView?.Refresh();
            NotifyPropertyChanged(nameof(ItemsCount));
        }

    }
}



