﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
using TAS.Client.Common;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.MediaDirectory;
using TAS.Client.Common.Plugin;
using resources = TAS.Client.Common.Properties.Resources;

namespace TAS.Client.ViewModels
{
    public class MediaSearchViewmodel : ViewModelBase, IUiPreviewProvider
    {
        private readonly TMediaType _mediaType;
        private readonly VideoFormatDescription _videoFormatDescription;
        private IWatcherDirectory _searchDirectory;
        private ICollectionView _itemsView;
        public readonly VideoLayer Layer;
        public PreviewViewmodel _preview;

        private IEvent _baseEvent;
        private string[] _searchTextSplit = new string[0];
        private string _searchText = string.Empty;
        private object _mediaCategory;
        private MediaViewViewmodel _selectedItem;
        private string _okButtonText = "OK";
        internal TStartType NewEventStartType;
        private ObservableCollection<MediaViewViewmodel> _items = new ObservableCollection<MediaViewViewmodel>();
        private bool _isRecursive;
        private bool _showExpired;

        public MediaSearchViewmodel(IPreview preview, IEngine engine, TMediaType mediaType, VideoLayer layer,
            bool closeAfterAdd, VideoFormatDescription videoFormatDescription)
        {
            Engine = engine;
            Layer = layer;
            if (mediaType == TMediaType.Movie)
            {
                _videoFormatDescription = engine.FormatDescription;
                if (preview != null)
                    _preview = new PreviewViewmodel(preview, false, false) { IsSegmentsVisible = true };
            }
            else
                _videoFormatDescription = videoFormatDescription;
            _mediaType = mediaType;
            if (_preview != null)
                _preview.PropertyChanged += PreviewViewModel_PropertyChanged;
            CommandAdd = new UiCommand(CommandName(nameof(Add)), Add, CanAdd);
            _mediaCategory = MediaCategories.FirstOrDefault();
            NewEventStartType = TStartType.After;
            SetupSearchDirectory(closeAfterAdd, mediaType);
        }

        private void SetupSearchDirectory(bool closeAfterAdd, TMediaType mediaType)
        {
            _searchDirectory = Engine.MediaManager.DetermineValidServerDirectory(mediaType == TMediaType.Animation);
            if (_searchDirectory == null)
                return;
            _searchDirectory.MediaAdded += SearchDirectory_MediaAdded;
            _searchDirectory.MediaRemoved += SearchDirectory_MediaRemoved;
            _searchDirectory.MediaVerified += SearchDirectory_MediaVerified;

            if (!closeAfterAdd)
                OkButtonText = resources._button_Add;
            Items = new ObservableCollection<MediaViewViewmodel>(
                _searchDirectory.GetAllFiles()
                    .Where(m => CanAddMediaToCollection(m, mediaType))
                    .Select(m => new MediaViewViewmodel(m, Engine.MediaManager)));
            _itemsView = CollectionViewSource.GetDefaultView(Items);
            _itemsView.Filter += ItemsFilter;

            IsRecursive = _searchDirectory is IServerDirectory sd && sd.IsRecursive;

            if (mediaType == TMediaType.Movie || mediaType == TMediaType.Audio)
                SortByIngestDate();
            else
                SortByName();
        }

        public ObservableCollection<MediaViewViewmodel> Items { get => _items; private set => SetField(ref _items, value); }

        public ICommand CommandAdd { get; }

        public Views.MediaSearchView Window { get; set; }

        public string SearchText
        {
            get => _searchText;
            set
            {
                var oldValue = _searchText;
                value = value.ToLower();
                if (!SetField(ref _searchText, value))
                    return;
                _searchTextSplit = value.Split(' ');
                if (!UserSorted && (_mediaType == TMediaType.Audio || _mediaType == TMediaType.Movie))
                {
                    if (string.IsNullOrWhiteSpace(value) && !string.IsNullOrWhiteSpace(oldValue))
                        SortByIngestDate();
                    if (!string.IsNullOrWhiteSpace(value) && string.IsNullOrWhiteSpace(oldValue))
                        SortByName();
                }
                _itemsView?.Refresh();
                _itemsView?.MoveCurrentToFirst();
                SelectedItem = _itemsView?.CurrentItem as MediaViewViewmodel;
            }
        }

        public IEnumerable<object> MediaCategories { get; } =
            new List<object> { resources._all_ }.Concat(Enum.GetValues(typeof(TMediaCategory)).Cast<object>());

        public object MediaCategory
        {
            get => _mediaCategory;
            set
            {
                if (SetField(ref _mediaCategory, value))
                {
                    NotifyPropertyChanged(nameof(IsShowCategoryColumn));
                    _itemsView?.Refresh();
                }
            }
        }

        public bool ShowExpired
        {
            get => _showExpired;
            set
            {
                if (!SetField(ref _showExpired, value))
                    return;
                _itemsView?.Refresh();
            }
        }

        public bool IsRecursive { get => _isRecursive; private set => SetField(ref _isRecursive, value); }

        public IMedia SelectedMedia => _selectedItem?.Media;

        public MediaViewViewmodel SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (value != _selectedItem)
                {
                    _selectedItem = value;
                    var media = SelectedMedia;
                    if (_preview != null)
                        _preview.SelectedMedia = media;
                    InvalidateRequerySuggested();
                }
            }
        }

        public bool IsMovie => _mediaType == TMediaType.Movie;

        public bool IsShowCategoryColumn => _mediaType == TMediaType.Movie && !(_mediaCategory is TMediaCategory);


        public bool CanEnableCGElements => Engine.CGElementsController != null && _mediaType == TMediaType.Movie;

        public string OkButtonText
        {
            get => _okButtonText;
            set
            {
                if (value == _okButtonText)
                    return;
                _okButtonText = value;
                NotifyPropertyChanged(nameof(OkButtonText));
            }
        }

        public event EventHandler<MediaSearchEventArgs> MediaChoosen;

        internal IEvent BaseEvent
        {
            get => _baseEvent;
            set
            {
                IEvent b = _baseEvent;
                if (b != value)
                {
                    if (b != null)
                        b.PropertyChanged -= BaseEvent_PropertyChanged;
                    _baseEvent = value;
                    InvalidateRequerySuggested();
                    if (value != null)
                        value.PropertyChanged += BaseEvent_PropertyChanged;
                }
            }
        }

        public IEngine Engine { get; }

        public bool UserSorted { get; set; }

        public IUiPreview Preview => _preview;

        private bool CanAddMediaToCollection(IMedia media, TMediaType requiredMediaType)
        {
            return
                media != null
                && media.MediaType == requiredMediaType
                &&
                   ((requiredMediaType == TMediaType.Still && (_videoFormatDescription == null || _videoFormatDescription.IsWideScreen == media.FormatDescription().IsWideScreen))
                 || (media.MediaType == TMediaType.Movie && media.FrameRate().Equals(_videoFormatDescription?.FrameRate))
                 || media.MediaType == TMediaType.Animation);
        }

        private void SearchDirectory_MediaVerified(object _, MediaEventArgs e)
        {
            OnUiThread(() => _itemsView?.Refresh());
        }

        private void SearchDirectory_MediaRemoved(object _, MediaEventArgs e)
        {
            OnUiThread(() =>
            {
                var mvm = Items.FirstOrDefault(m => m.Media == e.Media);
                if (mvm == null)
                    return;
                Items.Remove(mvm);
                mvm.Dispose();
                _itemsView?.Refresh();
            });
        }

        private void SearchDirectory_MediaAdded(object _, MediaEventArgs e)
        {
            OnUiThread(() =>
            {
                IMedia media = e.Media;
                if (media != null
                    && CanAddMediaToCollection(media, _mediaType))
                    Items.Add(new MediaViewViewmodel(media, Engine.MediaManager));
            });
        }

        private bool ItemsFilter(object item)
        {
            var mvm = item as MediaViewViewmodel;
            if (mvm?.Media == null)
                return false;
            string mediaName = mvm.MediaName.ToLower();
            return mvm.MediaStatus == TMediaStatus.Available
                && (!(MediaCategory is TMediaCategory) || (MediaCategory as TMediaCategory?) == mvm.MediaCategory)
                && (ShowExpired || !mvm.IsExpired)
                && (_searchTextSplit.All(s => mediaName.Contains(s)));
        }

        private TimeSpan GetTCStart()
        {
            var pvlm = _preview?.LoadedMedia;
            if (pvlm != null && pvlm.MediaGuid == SelectedMedia?.MediaGuid)
            {
                var s = _preview.SelectedSegment;
                if (s != null)
                    return s.TcIn;
                if (_preview.TcIn != pvlm.TcPlay ||
                    _preview.TcOut != pvlm.TcPlay + pvlm.DurationPlay)
                    return _preview.TcIn;
            }
            return SelectedItem?.SelectedSegment?.TcIn ?? SelectedItem?.TcPlay ?? TimeSpan.Zero;
        }

        private TimeSpan GetDuration()
        {
            var pvlm = _preview?.LoadedMedia;
            if (pvlm != null && pvlm.MediaGuid == SelectedMedia?.MediaGuid)
            {
                var s = _preview.SelectedSegment;
                if (s != null)
                    return s.Duration;
                if (_preview.TcIn != pvlm.TcPlay ||
                    _preview.TcOut != pvlm.TcPlay + pvlm.DurationPlay)
                    return new TimeSpan(_preview.TcOut.Ticks - _preview.TcIn.Ticks + _preview.FormatDescription.FrameTicks);
            }
            return SelectedItem?.SelectedSegment?.Duration ?? SelectedItem?.DurationPlay ?? TimeSpan.Zero;
        }

        private string GetMediaName()
        {
            var pvlm = _preview?.LoadedMedia;
            if (pvlm != null)
            {
                var s = _preview.SelectedSegment;
                if (s != null)
                    return pvlm.MediaName + " [" + s.SegmentName + "]";
                if (_preview.TcIn != pvlm.TcPlay ||
                    _preview.TcOut != pvlm.TcPlay + pvlm.DurationPlay)
                    return pvlm.MediaName + " [fragment]";
            }

            if (SelectedItem == null)
                return string.Empty;
            return SelectedItem.SelectedSegment == null ? SelectedItem.MediaName : $"{SelectedItem.MediaName} [{SelectedItem.SelectedSegment.SegmentName}]";
        }

        private bool CanAdd(object _)
        {
            if (SelectedMedia == null)
                return false;
            if (BaseEvent == null)
                return true;
            var loadedMedia = _preview?.LoadedMedia;
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

        private void BaseEvent_PropertyChanged(object _, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IEvent.PlayState))
                InvalidateRequerySuggested();
        }

        private void PreviewViewModel_PropertyChanged(object _, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_preview.LoadedMedia))
                InvalidateRequerySuggested();
        }

        private void Add(object _)
        {
            var sm = SelectedItem;
            var handler = MediaChoosen;
            if (handler != null && sm != null)
                handler(this, new MediaSearchEventArgs(sm.Media, sm.SelectedSegment?.MediaSegment, GetMediaName(), GetTCStart(), GetDuration()));
        }

        protected override void OnDispose()
        {
            BaseEvent = null;
            if (_preview != null)
            {
                _preview.PropertyChanged -= PreviewViewModel_PropertyChanged;
                _preview.Dispose();
            }
            if (_searchDirectory != null)
            {
                _searchDirectory.MediaAdded -= SearchDirectory_MediaAdded;
                _searchDirectory.MediaRemoved -= SearchDirectory_MediaRemoved;
                _searchDirectory.MediaVerified -= SearchDirectory_MediaVerified;
            }
            foreach (var item in Items)
                item.Dispose();
            Debug.WriteLine("MediaSearchViewModel disposed");
        }

        private void SortByName()
        {
            _itemsView?.SortDescriptions.Clear();
            _itemsView?.SortDescriptions.Add(new SortDescription(nameof(MediaViewViewmodel.MediaName), ListSortDirection.Ascending));
        }

        private void SortByIngestDate()
        {
            _itemsView?.SortDescriptions.Clear();
            _itemsView?.SortDescriptions.Add(new SortDescription(nameof(MediaViewViewmodel.LastUpdated), ListSortDirection.Descending));
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
