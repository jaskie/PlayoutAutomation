using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows;
using System.Collections;
using System.Collections.ObjectModel;
using System.Dynamic;
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
using System.Diagnostics;

namespace TAS.Client.ViewModels
{
    public class MediaManagerViewmodel : ViewModelBase, IUiPluginContext, IUiPreviewProvider
    {
        private const int MinSearchLength = 3;
        private readonly IMediaManager _mediaManager;
        bool _isDisplayPreview;
        private PreviewViewmodel _preview;
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
        private IMediaSearchProvider _currentSearchProvider;
        private bool _isSearching;

        public MediaManagerViewmodel(IEngine engine, IPreview preview)
        {
            _mediaManager = engine.MediaManager;
            Engine = engine;
            if (preview != null)
                _preview = new PreviewViewmodel(preview, engine.HaveRight(EngineRight.MediaEdit), true);

            MediaDirectories = new List<MediaDirectoryViewmodel>();
            MediaDirectories.AddRange(_mediaManager.IngestDirectories.Where(d => d.ContainsImport()).Select(d => new MediaDirectoryViewmodel(d, d.DirectoryName, true)));
            if (_mediaManager.ArchiveDirectory != null)
            {
                MediaDirectories.Insert(0, new MediaDirectoryViewmodel(_mediaManager.ArchiveDirectory, resources._archive));
                _mediaManager.ArchiveDirectory.MediaIsArchived += ArchiveDirectory_MediaIsArchived;
            }
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
            {
                MediaDirectories.Insert(0, new MediaDirectoryViewmodel(serverDirectoryPri, resources._primary));
                serverDirectoryPri.IngestStatusUpdated += ServerDirectoryPri_IngestStatusUpdated;
            }

            _mediaCategory = MediaCategories.FirstOrDefault();
            SelectedDirectory = MediaDirectories.FirstOrDefault();
            if (_mediaManager.FileManager != null)
                FileManagerViewmodel = new FileManagerViewmodel(_mediaManager);
            RecordersViewmodel = new RecordersViewmodel(Engine, _mediaManager.Recorders);
            RecordersViewmodel.PropertyChanged += RecordersViewmodel_PropertyChanged;
            VideoPreview = UiPluginManager.ComposePart<IVideoPreview>(this);
            VideoPreview?.SetSource(RecordersViewmodel.Channel?.PreviewUrl);

            CommandSearch = new UiCommand(Search, CanSearch);
            CommandClearFilters = new UiCommand(ClearFilters, CanClearFilters);
            CommandDeleteSelected = new UiCommand(DeleteSelected, o => IsSomethingSelected() && engine.HaveRight(EngineRight.MediaDelete));
            CommandIngestSelectedToServer = new UiCommand(IngestSelectedToServer, CanIngestSelectedToServer);
            CommandMoveSelectedToArchive = new UiCommand(MoveSelectedToArchive, o => _selectedDirectory != null && _selectedDirectory.IsServerDirectory && IsSomethingSelected() && engine.HaveRight(EngineRight.MediaArchive) && engine.HaveRight(EngineRight.MediaDelete));
            CommandCopySelectedToArchive = new UiCommand(CopySelectedToArchive, o => _selectedDirectory != null && _selectedDirectory.IsServerDirectory && IsSomethingSelected() && engine.HaveRight(EngineRight.MediaArchive));
            CommandSweepStaleMedia = new UiCommand(SweepStaleMedia, o => CurrentUser.IsAdmin);
            CommandGetLoudness = new UiCommand(GetLoudness, o => IsSomethingSelected() && engine.HaveRight(EngineRight.MediaEdit));
            CommandExport = new UiCommand(Export, CanExport);
            CommandRefresh = new UiCommand(ob => ReloadFiles(), CanRefresh);
            CommandSyncPriToSec = new UiCommand(SyncSecToPri, o => (_selectedDirectory.IsServerDirectory || _selectedDirectory.IsAnimationDirectory) && CurrentUser.IsAdmin);
            CommandCloneAnimation = new UiCommand(CloneAnimation, CanCloneAnimation);
            CommandTogglePropertiesPanel = new UiCommand(o => IsPropertiesPanelVisible = !IsPropertiesPanelVisible);
            CommandVerifyAllMedia = new UiCommand(VerifyAllMedia, o => _selectedDirectory.IsServerDirectory && CurrentUser.IsAdmin);
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
        public ICommand CommandVerifyAllMedia { get; }
        public ICommand CommandCloneAnimation { get; }
        public ICommand CommandTogglePropertiesPanel { get; }


        #region PreviewCommands
        public ICommand CommandPreviewCue => _preview?.CommandCue;
        public ICommand CommandPreviewTogglePlay => _preview?.CommandTogglePlay;
        public ICommand CommandPreviewUnload => _preview?.CommandUnload;
        public ICommand CommandPreviewFastForward => _preview?.CommandFastForward;
        public ICommand CommandPreviewBackward => _preview?.CommandBackward;
        public ICommand CommandPreviewFastForwardOneFrame => _preview?.CommandFastForwardOneFrame;
        public ICommand CommandPreviewBackwardOneFrame => _preview?.CommandBackwardOneFrame;
        public ICommand CommandPreviewTrimSource => _preview?.CommandTrimSource;
        #endregion

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
                if (!CheckEditMediaSaved())
                {
                    NotifyPropertyChanged(nameof(SelectedMediaVm));
                    return;
                }
                var oldSelectedMedia = _selectedMediaVm;
                if (!SetField(ref _selectedMediaVm, value))
                    return;
                SelectedMedia = value?.Media;
                if (oldSelectedMedia != null)
                    oldSelectedMedia.SelectedSegment = null;
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
                if (_preview != null)
                    _preview.SelectedMedia = value;
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
                if (!SetField(ref _mediaCategory, value))
                    return;
                NotifyPropertyChanged(nameof(IsDisplayMediaCategory));
                Search(null);
            }
        }

        public IEnumerable<object> MediaTypes => new List<object> { resources._all_ }.Concat(Enum.GetValues(typeof(TMediaType)).Cast<object>());
        public object MediaType
        {
            get => _mediaType;
            set
            {
                if (!SetField(ref _mediaType, value))
                    return;
                NotifyPropertyChanged(nameof(IsMediaCategoryVisible));
                Search(null);
            }
        }

        public List<MediaDirectoryViewmodel> MediaDirectories { get; }

        public MediaDirectoryViewmodel SelectedDirectory
        {
            get => _selectedDirectory;
            set
            {
                if (CheckEditMediaSaved())
                {
                    var oldSelectedDirectory = SelectedDirectory;
                    if (!SetField(ref _selectedDirectory, value))
                        return;
                    if (oldSelectedDirectory != null)
                    {
                        oldSelectedDirectory.Directory.MediaAdded -= SelectedDirectory_MediaAdded;
                        oldSelectedDirectory.Directory.MediaRemoved -= SelectedDirectory_MediaRemoved;
                        oldSelectedDirectory.Directory.PropertyChanged -= SelectedDirectory_PropertyChanged;
                    }
                    if (value != null)
                    {
                        value.Directory.MediaAdded += SelectedDirectory_MediaAdded;
                        value.Directory.MediaRemoved += SelectedDirectory_MediaRemoved;
                        value.Directory.PropertyChanged += SelectedDirectory_PropertyChanged;
                        ReloadFiles();
                        SetupPreview(value.Directory);
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
                    NotifyPropertyChanged(nameof(IsXdcam));
                    NotifyDirectoryPropertiesChanged();
                    InvalidateRequerySuggested();
                }
                else
                    OnIdle(() => NotifyPropertyChanged(nameof(SelectedDirectory))); //revert folder display, deferred execution
            }
        }

        public bool IsPropertiesPanelVisible
        {
            get => _isPropertiesPanelVisible;
            set => SetField(ref _isPropertiesPanelVisible, value);
        }

        public bool IsDisplayFolder => _selectedDirectory?.IsRecursive ?? false;

        public bool IsXdcam => _selectedDirectory?.IsXdcam ?? false;

        public bool IsDisplayMediaCategory => _selectedDirectory?.IsPersistentDirectory == true && !(_mediaCategory is TMediaCategory);

        public bool IsMediaCategoryVisible => _selectedDirectory?.IsPersistentDirectory == true && (!(_mediaType is TMediaType) || Equals(_mediaType, TMediaType.Movie));

        public bool IsServerDirectory => _selectedDirectory?.IsServerDirectory ?? false;

        public bool IsIngestOrArchiveDirectory => _selectedDirectory != null && (_selectedDirectory.IsArchiveDirectory || _selectedDirectory.IsIngestDirectory);

        public bool IsAnimationDirectory => _selectedDirectory?.IsAnimationDirectory ?? false;

        public bool IsMediaExportVisible { get { return MediaDirectories.Any(d => d.IsExport) && Engine.HaveRight(EngineRight.MediaExport); } }

        public bool DisplayDirectoryInfo => _selectedDirectory != null
                                            && (_selectedDirectory.IsServerDirectory || _selectedDirectory.IsArchiveDirectory || (_selectedDirectory.IsIngestDirectory && (_selectedDirectory.AccessType == TDirectoryAccessType.Direct || _selectedDirectory.IsXdcam)));

        public bool IsMediaDirectoryOk => _selectedDirectory?.IsOK ?? false;

        public float DirectoryTotalSpace => _selectedDirectory?.VolumeTotalSize / 1073741824F ?? 0F;

        public float DirectoryFreeSpace => _selectedDirectory?.VolumeFreeSize / 1073741824F ?? 0F;

        public float DirectoryFreePercentage => _selectedDirectory?.DirectoryFreePercentage ?? 0;

        public int ItemsCount => _mediaItems?.Count(MediaItemFilter) ?? 0;

        public ICollectionView MediaItemsView { get => _mediaItemsView; private set => SetField(ref _mediaItemsView, value); }

        public bool IsShowRecorders => RecordersViewmodel.Recorders.Any();


        protected override void OnDispose()
        {
            SelectedDirectory = null;
            if (RecordersViewmodel != null)
                RecordersViewmodel.PropertyChanged -= RecordersViewmodel_PropertyChanged;
            if (_mediaManager.ArchiveDirectory != null)
                _mediaManager.ArchiveDirectory.MediaIsArchived -= ArchiveDirectory_MediaIsArchived;
            if (_mediaManager.MediaDirectoryPRI != null)
                _mediaManager.MediaDirectoryPRI.IngestStatusUpdated -= ServerDirectoryPri_IngestStatusUpdated;
        }

        // private methods

        private bool CheckAskMediaDelete(IEnumerable<MediaDeleteResult> results)
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

        private void DeleteSelected(object o)
        {
            var selections = GetSelections();
            if (MessageBox.Show(
                    string.Format(resources._query_DeleteSelectedFiles, selections.Select(m => $"{m.Directory.GetDisplayName(_mediaManager)}:{m.MediaName}").AsString(Environment.NewLine)),
                    resources._caption_Confirmation, MessageBoxButton.OKCancel) != MessageBoxResult.OK)
                return;
            var reasons = _mediaManager.MediaDelete(selections, false);
            if (CheckAskMediaDelete(reasons))
                _mediaManager.MediaDelete(reasons.Where(r => r.Result != MediaDeleteResult.MediaDeleteResultEnum.Success).Select(r => r.Media).ToArray(), true);
        }

        private void MoveSelectedToArchive(object o)
        {
            var selections = GetSelections();
            var reasons = _mediaManager.MediaArchive(selections, true, false);
            if (CheckAskMediaDelete(reasons))
                _mediaManager.MediaArchive(reasons.Where(r => r.Result != MediaDeleteResult.MediaDeleteResultEnum.Success).Select(r => r.Media).ToArray(), true, true);
        }

        private void CopySelectedToArchive(object o)
        {
            _mediaManager.MediaArchive(GetSelections(), false, false);
        }

        private bool MediaItemFilter(object item)
        {
            var dir = _selectedDirectory?.Directory;
            if (dir?.HaveFileWatcher != true)
                return true;
            if (!(item is MediaViewViewmodel m))
                return false;
            var mediaName = m.MediaName?.ToLower() ?? string.Empty;
            return (!(dir is IServerDirectory) || !(_mediaCategory is TMediaCategory) || m.MediaCategory == (TMediaCategory)_mediaCategory)
                   && _searchTextSplit.All(s => mediaName.Contains(s))
                   && (!(_mediaType is TMediaType mediaType) || m.Media.MediaType == mediaType);
        }

        private bool CanRefresh(object obj)
        {
            return _selectedDirectory?.Directory is IIngestDirectory;
        }

        private void RecordersViewmodel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(RecordersViewmodel.RecordingMedia))
            {
                var media = ((RecordersViewmodel)sender).RecordingMedia;
                if (media != null)
                {
                    EditMedia = new MediaEditViewmodel(media, _mediaManager, true);
                    if (_preview != null)
                        _preview.SelectedMedia = media;
                }
            }
            if (e.PropertyName == nameof(RecordersViewmodel.Channel))
            {
                VideoPreview?.SetSource(((RecordersViewmodel)sender).Channel?.PreviewUrl);
            }
        }

        private void SelectedDirectory_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(IWatcherDirectory.IsInitialized):
                case nameof(IMediaDirectory.VolumeFreeSize):
                    NotifyDirectoryPropertiesChanged();
                    break;
            }
        }

        private async void ReloadFiles()
        {
            UiServices.SetBusyState();
#if DEBUG
            var stopwatch = Stopwatch.StartNew();
#endif
            CancelMediaSearchProvider();
            SetMediaItems(null);
            switch (SelectedDirectory.Directory)
            {
                case IServerDirectory serverDirectory:
                    SetMediaItems(serverDirectory.GetAllFiles());
                    break;
                case IAnimationDirectory animationDirectory:
                    SetMediaItems(animationDirectory.GetAllFiles());
                    break;
                case IArchiveDirectory archiveDirectory:
                    await Task.Run(() => StartMediaSearchProvider(archiveDirectory));
                    break;
                case IIngestDirectory ingestDirectory:
                    if (ingestDirectory.HaveFileWatcher)
                        SetMediaItems(ingestDirectory.GetAllFiles());
                    else
                    {
                        if (!CanSearch(null))
                            return;
                        try
                        {
                            await Task.Run(() => StartMediaSearchProvider(ingestDirectory));
                        }
                        catch (Exception e)
                        {
                            if (ingestDirectory == SelectedDirectory.Directory)
                                MessageBox.Show(string.Format(resources._message_DirectoryRefreshFailed, e.Message),
                                    resources._caption_Error, MessageBoxButton.OK, MessageBoxImage.Hand);
                        }
                    }
                    break;
            }
#if DEBUG
            Debug.WriteLine("MediaManagerViewmodel:ReloadFiles took {0} ms", stopwatch.ElapsedMilliseconds);
#endif
        }

        private void CancelMediaSearchProvider()
        {
            if (_currentSearchProvider == null)
                return;
            _currentSearchProvider.ItemAdded -= Search_ItemAdded;
            _currentSearchProvider.Finished -= Search_Finished;
            _currentSearchProvider.Cancel();
            _currentSearchProvider.Dispose();
            _currentSearchProvider = null;
            IsSearching = false;
        }

        private void StartMediaSearchProvider(IMediaDirectory mediaDirectory)
        {
            var newSearch = mediaDirectory.Search(MediaCategory as TMediaCategory?, SearchText?.ToLower());
            _currentSearchProvider = newSearch;
            newSearch.ItemAdded += Search_ItemAdded;
            newSearch.Finished += Search_Finished;
            IsSearching = true;
            newSearch.Start();
        }

        private void Search_ItemAdded(object sender, EventArgs<IMedia> e)
        {
            if (_currentSearchProvider == null)
                return;
            OnUiThread(() => AddMediaToItems(e.Value));
        }

        private void Search_Finished(object sender, EventArgs e)
        {
            if (!(sender is IMediaSearchProvider provider))
                return;
            provider.Dispose();
            provider.ItemAdded -= Search_ItemAdded;
            provider.Finished -= Search_Finished;
            if (provider != _currentSearchProvider)
                return;
            _currentSearchProvider = null;
            IsSearching = false;
        }

        public bool IsSearching { get => _isSearching; set => SetField(ref _isSearching, value); }

        public IUiPreview Preview => _preview;

        private void SetMediaItems(IEnumerable<IMedia> items)
        {
            var newItems = items == null
                ? new ObservableCollection<MediaViewViewmodel>()
                : new ObservableCollection<MediaViewViewmodel>(items.Select(media => new MediaViewViewmodel(media, _mediaManager)));
            var oldMediaItems = _mediaItems;
            _mediaItems = newItems;
            if (oldMediaItems != null)
                foreach (var m in oldMediaItems)
                    m.Dispose();
            SelectedMediaVm = null;
            MediaItemsView = CollectionViewSource.GetDefaultView(newItems);
            if (items != null)
                MediaItemsView.Filter = MediaItemFilter;
            MediaItemsView.SortDescriptions.Add(!SelectedDirectory.IsXdcam
                ? new SortDescription(nameof(MediaViewViewmodel.LastUpdated), ListSortDirection.Descending)
                : new SortDescription(nameof(MediaViewViewmodel.ClipNr), ListSortDirection.Ascending));
            NotifyPropertyChanged(nameof(ItemsCount));
        }

        private void AddMediaToItems(IMedia media)
        {
            _mediaItems?.Add(new MediaViewViewmodel(media, _mediaManager));
            NotifyDirectoryPropertiesChanged();
        }

        private void SelectedDirectory_MediaAdded(object source, MediaEventArgs e)
        {
            if (source is IWatcherDirectory dir && dir.IsInitialized)
                OnUiThread(() => AddMediaToItems(e.Media));
        }

        private void SelectedDirectory_MediaRemoved(object source, MediaEventArgs e)
        {
            if (source is IWatcherDirectory dir && dir.IsInitialized || source is IArchiveDirectory)
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
                    NotifyDirectoryPropertiesChanged();
                });
        }

        private void NotifyDirectoryPropertiesChanged()
        {
            NotifyPropertyChanged(nameof(ItemsCount));
            NotifyPropertyChanged(nameof(IsMediaDirectoryOk));
            NotifyPropertyChanged(nameof(DirectoryFreePercentage));
            NotifyPropertyChanged(nameof(DirectoryTotalSpace));
            NotifyPropertyChanged(nameof(DirectoryFreeSpace));
        }

        private List<IMedia> GetSelections()
        {
            return _selectedMediaList?.OfType<MediaViewViewmodel>().Select(m => m.Media).ToList() ?? new List<IMedia>();
        }

        private bool IsSomethingSelected()
        {
            return _selectedMediaList != null && _selectedMediaList.Count > 0;
        }

        private bool CanIngestSelectedToServer(object o)
        {
            return _selectedDirectory != null && Engine.HaveRight(EngineRight.MediaIngest) && (_selectedDirectory.IsIngestDirectory || _selectedDirectory.IsArchiveDirectory) && IsSomethingSelected();
        }

        private bool CanExport(object o)
        {
            return _selectedMediaList != null && _selectedMediaList.Cast<MediaViewViewmodel>().Any(m => m.Media.MediaType == TMediaType.Movie);
        }

        private bool CanClearFilters(object obj)
        {
            return IsMediaCategoryVisible && _mediaCategory != MediaCategories.FirstOrDefault()
                   || _mediaType != MediaTypes.FirstOrDefault()
                   || !string.IsNullOrWhiteSpace(_searchText);
        }

        private void ClearFilters(object obj)
        {
            _mediaCategory = MediaCategories.FirstOrDefault();
            _mediaType = MediaTypes.FirstOrDefault();
            NotifyPropertyChanged(nameof(MediaCategory));
            NotifyPropertyChanged(nameof(MediaType));
            SearchText = string.Empty;
            if (CanSearch(obj))
                Search(obj);
        }

        private void CloneAnimation(object obj)
        {
            var dir = _selectedDirectory?.Directory as IAnimationDirectory;
            if (_selectedMediaVm?.Media is IAnimatedMedia media)
                dir?.CloneMedia(media, Guid.NewGuid());
        }

        private bool CanCloneAnimation(object obj)
        {
            return _selectedMediaVm?.Media is IAnimatedMedia && Engine.HaveRight(EngineRight.MediaEdit);
        }


        private void SyncSecToPri(object o)
        {
            if (_selectedDirectory?.Directory is IServerDirectory)
                _mediaManager.SynchronizeMediaSecToPri();
            if (_selectedDirectory?.Directory is IAnimationDirectory)
                _mediaManager.SynchronizeAnimationsPropertiesSecToPri();
        }

        private async void VerifyAllMedia(object o)
        {
            foreach (var media in _mediaItems.Where(m => !m.IsVerified).Select(m => m.Media).ToArray())
                await Task.Run(() => media.Verify(true));
        }

        private void Export(object obj)
        {
            var selections = GetSelections().Select(m => new MediaExportDescription(m, new List<IMedia>(), m.TcPlay, m.DurationPlay, m.AudioVolume));
            using (var vm = new ExportViewmodel(Engine, selections))
            {
                UiServices.ShowDialog<Views.ExportView>(vm);
            }
        }

        private void SweepStaleMedia(object o)
        {
            SelectedDirectory.SweepStaleMedia();
        }

        private void GetLoudness(object o)
        {
            _mediaManager.MeasureLoudness(GetSelections());
        }

        private void IngestSelectionToDir()
        {
            if (!(_selectedDirectory?.Directory is IIngestDirectory currentDir))
                return;
            var selectedMediaList = GetSelections();
            Task.Run(() =>
            {
                foreach (var media in selectedMediaList.Where(m => !m.IsVerified))
                    media.Verify(true);
            });
            var ingestList = new List<IIngestOperation>(selectedMediaList.Where(sm => sm is IIngestMedia).Select(sourceMedia =>
            {
                var operation = (IIngestOperation)_mediaManager.FileManager.CreateFileOperation(TFileOperationKind.Ingest);
                operation.Source = sourceMedia;
                operation.DestDirectory = _mediaManager.DetermineValidServerDirectory();
                operation.AudioVolume = currentDir.AudioVolume;
                operation.SourceFieldOrderEnforceConversion = currentDir.SourceFieldOrder;
                operation.AspectConversion = currentDir.AspectConversion;
                operation.LoudnessCheck = currentDir.MediaLoudnessCheckAfterIngest;
                return operation;
            }));
            if (ingestList.Count == 0)
                return;
            using (var ievm = new IngestEditorViewmodel(ingestList, Engine))
            {
                if (UiServices.ShowDialog<Views.IngestEditorView>(ievm) == true)
                    ievm.ScheduleAll();
            }
        }

        private bool CheckEditMediaSaved()
        {
            if (EditMedia == null || !EditMedia.IsModified)
                return true;
            switch (MessageBox.Show(string.Format(resources._query_SaveChangedData, EditMedia), resources._caption_Confirmation, MessageBoxButton.YesNoCancel))
            {
                case MessageBoxResult.Cancel:
                    return false;
                case MessageBoxResult.Yes:
                    EditMedia.Save();
                    break;
            }
            return true;
        }

        private void IngestSelectedToServer(object o)
        {
            if (!CheckEditMediaSaved())
                return;
            UiServices.SetBusyState();
            if (_selectedDirectory.IsIngestDirectory)
                IngestSelectionToDir();
            else
                _mediaManager.CopyMediaToPlayout(GetSelections());
        }

        private bool CanSearch(object o)
        {
            var dir = _selectedDirectory?.Directory;
            if (dir == null)
                return false;
            return dir.HaveFileWatcher
                   || dir is IArchiveDirectory
                   || (dir is IIngestDirectory ingestDirectory && (!(ingestDirectory.Kind == TIngestDirectoryKind.SimpleFolder && ingestDirectory.IsWAN) || _searchText.Length >= MinSearchLength));
        }

        private void Search(object o)
        {
            var dir = SelectedDirectory?.Directory;
            if (dir == null)
                return;
            if (dir.HaveFileWatcher)
            {
                MediaItemsView?.Refresh();
                NotifyPropertyChanged(nameof(ItemsCount));
                return;
            }
            ReloadFiles();
        }

        private void SetupPreview(IMediaDirectory directory)
        {
            if (_preview == null)
                return;
            IsDisplayPreview = directory is IServerDirectory ||
                               directory is IArchiveDirectory ||
                               (directory is IIngestDirectory ingestDirectory && ingestDirectory.AccessType == TDirectoryAccessType.Direct);
            _preview.IsSegmentsVisible = directory is IServerDirectory || directory is IArchiveDirectory;
        }

        private void ArchiveDirectory_MediaIsArchived(object sender, MediaIsArchivedEventArgs e)
        {
            var vm = _mediaItems?.FirstOrDefault(m => m.Media.MediaGuid == e.Media.MediaGuid);
            if (vm == null)
                return;
            vm.IsArchived = e.IsArchived;
        }

        private void ServerDirectoryPri_IngestStatusUpdated(object sender, MediaIngestStatusEventArgs e)
        {
            if (e.Media.Directory != _selectedDirectory.Directory)
                return;
            var vm = _mediaItems?.FirstOrDefault(m => m.Media == e.Media);
            if (vm == null)
                return;
            vm.IngestStatus = e.IngestStatus;
        }

    }
}



