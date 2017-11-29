using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using System.Windows;
using System.ComponentModel;
using System.Windows.Data;
using System.Diagnostics;
using System.Windows.Input;
using TAS.Client.Common;
using TAS.Common;
using TAS.Common.Interfaces;
using resources = TAS.Client.Common.Properties.Resources;

namespace TAS.Client.ViewModels
{
    public class MediaSearchViewmodel : ViewmodelBase
    {
        private readonly IMediaManager _manager;
        private readonly TMediaType _mediaType;
        private readonly RationalNumber? _frameRate;
        private readonly VideoFormatDescription _videoFormatDescription;
        private readonly IEngine _engine;
        private readonly IMediaDirectory _searchDirectory;
        public readonly VideoLayer Layer;

        private IEvent _baseEvent;
        private string[] _searchTextSplit = new string[0];
        private string _searchText = string.Empty;
        private object _mediaCategory;
        private MediaViewViewmodel _selectedItem;
        private readonly ICollectionView _itemsView;
        private string _okButtonText = "OK";

        public MediaSearchViewmodel(IPreview preview, IMediaManager manager, TMediaType mediaType, VideoLayer layer,
            bool closeAfterAdd, VideoFormatDescription videoFormatDescription)
        {
            _manager = manager;
            _engine = manager.Engine;
            Layer = layer;
            if (mediaType == TMediaType.Movie)
            {
                _videoFormatDescription = manager.FormatDescription;
                _frameRate = _videoFormatDescription.FrameRate;
                if (preview != null)
                    PreviewViewmodel = new PreviewViewmodel(preview) {IsSegmentsVisible = true};
            }
            else
            {
                _videoFormatDescription = videoFormatDescription;
                _frameRate = videoFormatDescription?.FrameRate;
            }
            _mediaType = mediaType;
            if (PreviewViewmodel != null)
                PreviewViewmodel.PropertyChanged += _onPreviewViewModelPropertyChanged;
            IMediaDirectory pri = mediaType == TMediaType.Animation
                ? (IMediaDirectory) _manager.AnimationDirectoryPRI
                : _manager.MediaDirectoryPRI;
            IMediaDirectory sec = mediaType == TMediaType.Animation
                ? (IMediaDirectory) _manager.AnimationDirectorySEC
                : _manager.MediaDirectorySEC;
            _searchDirectory = pri != null && pri.DirectoryExists()
                ? pri
                : sec != null && sec.DirectoryExists()
                    ? sec
                    : null;
            if (_searchDirectory != null)
            {
                _searchDirectory.MediaAdded += _searchDirectory_MediaAdded;
                _searchDirectory.MediaRemoved += _searchDirectory_MediaRemoved;
                _searchDirectory.MediaVerified += _searchDirectory_MediaVerified;
            }
            _mediaCategory = MediaCategories.FirstOrDefault();
            NewEventStartType = TStartType.After;
            if (!closeAfterAdd)
                OkButtonText = resources._button_Add;
            _createCommands();
            Items = new ObservableCollection<MediaViewViewmodel>(_searchDirectory.GetFiles()
                .Where(m => _canAddMediaToCollection(m, mediaType))
                .Select(m => new MediaViewViewmodel(m)));
            _itemsView = CollectionViewSource.GetDefaultView(Items);
            _itemsView.SortDescriptions.Add(new SortDescription(nameof(MediaViewViewmodel.MediaName),
                ListSortDirection.Ascending));
            _itemsView.Filter += _itemsFilter;
        }

        public ObservableCollection<MediaViewViewmodel> Items { get; }

        public ICommand CommandAdd { get; private set; }

        public PreviewViewmodel PreviewViewmodel { get; }

        public Views.MediaSearchView Window { get; set; }

        public string SearchText
        {
            get { return _searchText; }
            set
            {
                value = value.ToLower();
                if (value != _searchText)
                {
                    _searchText = value;
                    _searchTextSplit = value.ToLower().Split(' ');
                    SelectedItem = null;
                    NotifyPropertyChanged(nameof(SearchText));
                    _itemsView.Refresh();
                }
            }
        }

        public IEnumerable<object> MediaCategories { get; } =
            new List<object> {resources._all_}.Concat(Enum.GetValues(typeof(TMediaCategory)).Cast<object>());

        public object MediaCategory
        {
            get { return _mediaCategory; }
            set
            {
                if (SetField(ref _mediaCategory, value))
                {
                    NotifyPropertyChanged(nameof(IsServerOrArchiveDirectory));
                    _itemsView.Refresh();
                }
            }
        }

        public IMedia SelectedMedia => _selectedItem?.Media;

        public MediaViewViewmodel SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                if (value != _selectedItem)
                {
                    _selectedItem = value;
                    IMedia media = SelectedMedia;
                    if (media is IIngestMedia
                        && ((IIngestDirectory) media.Directory).AccessType == TDirectoryAccessType.Direct
                        && !media.IsVerified)
                        media.ReVerify();
                    if (PreviewViewmodel != null)
                        PreviewViewmodel.SelectedMedia = media;
                    InvalidateRequerySuggested();
                }
            }
        }

        public bool IsMovie => _mediaType == TMediaType.Movie;

        public bool IsServerOrArchiveDirectory => _mediaType == TMediaType.Movie && !(_mediaCategory is TMediaCategory);

        public bool EnableCGElementsForNewEvents
        {
            get { return _engine.EnableCGElementsForNewEvents; }
            set { _engine.EnableCGElementsForNewEvents = value; }
        }

        public bool CanEnableCGElements => _engine.CGElementsController != null && _mediaType == TMediaType.Movie;

        public string OkButtonText
        {
            get { return _okButtonText; }
            set
            {
                if (value != _okButtonText)
                {
                    _okButtonText = value;
                    NotifyPropertyChanged(nameof(OkButtonText));
                }
            }
        }

        public event EventHandler<MediaSearchEventArgs> MediaChoosen;

        internal Action<MediaSearchEventArgs> ExecuteAction;

        internal TStartType NewEventStartType;

        internal IEvent BaseEvent
        {
            get => _baseEvent;
            set
            {
                IEvent b = _baseEvent;
                if (b != value)
                {
                    if (b != null)
                        b.PropertyChanged -= _onBaseEventPropertyChanged;
                    _baseEvent = value;
                    InvalidateRequerySuggested();
                    if (value != null)
                        value.PropertyChanged += _onBaseEventPropertyChanged;
                }
            }
        }

        private bool _canAddMediaToCollection(IMedia media, TMediaType requiredMediaType)
        {
            return
                media != null
                && media.MediaType == requiredMediaType
                &&
                   ((requiredMediaType == TMediaType.Still && media.FormatDescription().IsWideScreen == _videoFormatDescription?.IsWideScreen)
                 || (media.MediaType == TMediaType.Movie && media.FrameRate().Equals(_frameRate))
                 || media.MediaType == TMediaType.Animation);
        }

        private void _searchDirectory_MediaVerified(object sender, MediaEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)delegate
            {
                _itemsView.Refresh();
            });
        }

        private void _searchDirectory_MediaRemoved(object sender, MediaEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)delegate
            {
                var mvm = Items.FirstOrDefault(m => m.Media == e.Media);
                if (mvm != null)
                {
                    Items.Remove(mvm);
                    mvm.Dispose();
                    _itemsView.Refresh();
                }
            });
        }

        private void _searchDirectory_MediaAdded(object sender, MediaEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)delegate
            {
                IMedia media = e.Media;
                if (media != null 
                    && _canAddMediaToCollection(media, _mediaType))
                    Items.Add(new MediaViewViewmodel(media));
            });
        }

        private bool _itemsFilter(object item)
        {
            var mvm = item as MediaViewViewmodel;
            if (mvm?.Media == null)
                return false;
            string mediaName = mvm.MediaName.ToLower();
            return mvm.MediaStatus == TMediaStatus.Available
                && (!(MediaCategory is TMediaCategory) || (MediaCategory as TMediaCategory?) == mvm.MediaCategory)
                && (_searchTextSplit.All(s => mediaName.Contains(s)));
        }

        private void _createCommands()
        {
            CommandAdd = new UICommand { ExecuteDelegate = _add, CanExecuteDelegate = _allowAdd };
        }

        private TimeSpan GetTCStart()
        {
            if (PreviewViewmodel != null)
            {
                var pvlm = PreviewViewmodel.LoadedMedia;
                if (pvlm != null)
                {
                    var s = PreviewViewmodel.SelectedSegment;
                    if (s != null)
                        return s.TcIn;
                    if (PreviewViewmodel.TcIn != pvlm.TcPlay ||
                        PreviewViewmodel.TcOut != pvlm.TcPlay + pvlm.DurationPlay)
                        return PreviewViewmodel.TcIn;
                }
            }
            var mediaVm = SelectedItem;
            if (mediaVm != null)
            {
                var segmentVm = mediaVm.SelectedSegment;
                if (segmentVm == null)
                    return mediaVm.TcPlay;
                return segmentVm.TcIn;
            }
            return TimeSpan.Zero;
        }

        private TimeSpan GetDuration()
        {
            if (PreviewViewmodel != null)
            {
                var pvlm = PreviewViewmodel.LoadedMedia;
                if (pvlm != null)
                {
                    var s = PreviewViewmodel.SelectedSegment;
                    if (s != null)
                        return s.Duration;
                    if (PreviewViewmodel.TcIn != pvlm.TcPlay ||
                        PreviewViewmodel.TcOut != pvlm.TcPlay + pvlm.DurationPlay)
                        return PreviewViewmodel.DurationSelection;
                }
            }
            var mediaVm = SelectedItem;
            if (mediaVm != null)
            {
                var segmentVm = mediaVm.SelectedSegment;
                if (segmentVm == null)
                    return mediaVm.DurationPlay;
                return segmentVm.Duration;
            }
            return TimeSpan.Zero;
        }

        private string GetMediaName()
        {
            var pvlm = PreviewViewmodel?.LoadedMedia;
            if (pvlm != null)
            {
                var s = PreviewViewmodel.SelectedSegment;
                if (s != null)
                    return pvlm.MediaName + " [" + s.SegmentName + "]";
                if (PreviewViewmodel.TcIn != pvlm.TcPlay ||
                    PreviewViewmodel.TcOut != pvlm.TcPlay + pvlm.DurationPlay)
                    return pvlm.MediaName + " [fragment]";
            }

            if (SelectedItem == null)
                return string.Empty;
            return SelectedItem.SelectedSegment == null ? SelectedItem.MediaName : $"{SelectedItem.MediaName} [{SelectedItem.SelectedSegment.SegmentName}]";
        }

        private bool _allowAdd(object o)
        {
            if (SelectedMedia == null || BaseEvent == null)
                return false;
            var loadedMedia = PreviewViewmodel?.LoadedMedia;
            if (loadedMedia != null && SelectedMedia.MediaGuid != loadedMedia.MediaGuid)
                return false;
            switch (NewEventStartType)
            {
                case TStartType.WithParent:
                case TStartType.WithParentFromEnd:
                    switch (BaseEvent.EventType)
                    {
                        case TEventType.Movie:
                        case TEventType.Live:
                            return BaseEvent.PlayState == TPlayState.Scheduled || (_mediaType == TMediaType.Still && BaseEvent.PlayState == TPlayState.Playing);
                        case TEventType.Rundown:
                            return BaseEvent.PlayState == TPlayState.Scheduled && BaseEvent.SubEventsCount == 0;
                        default:
                            return false;
                    }
                case TStartType.After:
                    switch (BaseEvent.EventType)
                    {
                        case TEventType.Live:
                        case TEventType.Movie:
                            return BaseEvent.PlayState != TPlayState.Played;
                        case TEventType.Rundown:
                            var vp = BaseEvent.GetVisualParent();
                            return BaseEvent.PlayState != TPlayState.Played && vp != null &&
                                   vp.EventType != TEventType.Container;
                        default:
                            return false;
                    }
                default:
                    return false;
            }
        }

        private void _onBaseEventPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IEvent.PlayState))
                InvalidateRequerySuggested();
        }

        private void _onPreviewViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PreviewViewmodel.LoadedMedia))
                InvalidateRequerySuggested();
        }

        private  void _add(object param)
        {
            var sm = SelectedItem;
            var handler = MediaChoosen;
            if (handler!= null && sm != null)
                handler(this, new MediaSearchEventArgs(sm.Media, sm.SelectedSegment == null ? null : sm.SelectedSegment.MediaSegment, GetMediaName(), GetTCStart(), GetDuration()));
        }

        protected override void OnDispose()
        {
            BaseEvent = null;
            if (PreviewViewmodel != null)
            {
                PreviewViewmodel.PropertyChanged -= _onPreviewViewModelPropertyChanged;
                PreviewViewmodel.Dispose();
            }
            if (_searchDirectory != null)
            {
                _searchDirectory.MediaAdded -= _searchDirectory_MediaAdded;
                _searchDirectory.MediaRemoved -= _searchDirectory_MediaRemoved;
                _searchDirectory.MediaVerified -= _searchDirectory_MediaVerified;
            }
            _itemsView.Filter -= _itemsFilter;
            foreach (var item in Items)
                item.Dispose();
            Debug.WriteLine("MediaSearchViewModel disposed");
        }

    }

    public class MediaSearchEventArgs : EventArgs
    {
        public MediaSearchEventArgs(IMedia media, IMediaSegment segment, string mediaName, TimeSpan tCIn, TimeSpan duration)
        {
            Media = media;
            MediaSegment = segment;
            MediaName = mediaName;
            TCIn = tCIn;
            Duration = duration;
        }
        public IMedia Media { get; }
        public IMediaSegment MediaSegment {get; }
        public string MediaName { get; }
        public TimeSpan TCIn { get; }
        public TimeSpan Duration { get; }
    }
}
