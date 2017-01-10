using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Windows;
using System.ComponentModel;
using System.Windows.Data;
using System.Diagnostics;
using System.Windows.Input;
using System.Threading;
using TAS.Common;
using TAS.Client.Common;
using TAS.Server.Common;
using TAS.Server.Interfaces;
using resources = TAS.Client.Common.Properties.Resources;

namespace TAS.Client.ViewModels
{
    public class MediaSearchViewmodel: ViewmodelBase
    {
        private readonly IMediaManager _manager;
        private readonly PreviewViewmodel _previewViewmodel;
        private readonly TMediaType _mediaType;
        private readonly MediaSearchView _view;
        private readonly bool _closeAfterAdd;
        private readonly RationalNumber? _frameRate;
        private readonly VideoFormatDescription _videoFormatDescription;
        private readonly IEngine _engine;
        private readonly IMediaDirectory _searchDirectory;
        public readonly VideoLayer Layer;


        public MediaSearchViewmodel(IPreview preview, IMediaManager manager, TMediaType mediaType, VideoLayer layer, bool closeAfterAdd, VideoFormatDescription videoFormatDescription)
        {
            _manager = manager;
            _engine = manager.Engine;
            Layer = layer;
            if (mediaType == TMediaType.Movie)
            {
                _videoFormatDescription = manager.FormatDescription;
                _frameRate = _videoFormatDescription.FrameRate;
                if (preview != null)
                    _previewViewmodel = new PreviewViewmodel(preview) { IsSegmentsVisible = true };
                WindowWidth = _previewViewmodel != null ? 950 : 650;
            }
            else
            {
                _videoFormatDescription = videoFormatDescription;
                _frameRate = videoFormatDescription?.FrameRate;
                WindowWidth = 750;
            }
            _mediaType = mediaType;
            if (_previewViewmodel != null)
                _previewViewmodel.PropertyChanged += _onPreviewViewModelPropertyChanged;
            IMediaDirectory pri = mediaType == TMediaType.Animation ? (IMediaDirectory)_manager.AnimationDirectoryPRI : _manager.MediaDirectoryPRI;
            IMediaDirectory sec = mediaType == TMediaType.Animation ? (IMediaDirectory)_manager.AnimationDirectorySEC : _manager.MediaDirectorySEC;
            _searchDirectory = pri != null && pri.DirectoryExists() ? pri : sec != null && sec.DirectoryExists() ? sec : null;
            _searchDirectory.MediaAdded += _searchDirectory_MediaAdded;
            _searchDirectory.MediaRemoved += _searchDirectory_MediaRemoved;
            _searchDirectory.MediaVerified += _searchDirectory_MediaVerified;

            _closeAfterAdd = closeAfterAdd;
            _mediaCategory = MediaCategories.FirstOrDefault();
            NewEventStartType = TStartType.After;
            if (!closeAfterAdd)
                OkButtonText = resources._button_Add;
            _createCommands();
            _items = new ObservableCollection<MediaViewViewmodel>(_searchDirectory.GetFiles()
                .Where(m => _canAddMediaToCollection(m, mediaType))
                .Select(m => new MediaViewViewmodel(m)));
            _itemsView = CollectionViewSource.GetDefaultView(_items);
            _itemsView.SortDescriptions.Add(new SortDescription(nameof(MediaViewViewmodel.MediaName), ListSortDirection.Ascending));
            _itemsView.Filter += _itemsFilter;
            _view = new MediaSearchView(_frameRate ?? manager.FormatDescription.FrameRate);
            _view.Owner = System.Windows.Application.Current.Windows.OfType<System.Windows.Window>().FirstOrDefault(w => w.IsActive);
            _view.DataContext = this;
            _view.Closed += _windowClosed;
            _view.Show();
        }

        protected override void OnDispose()
        {
            BaseEvent = null;
            if (_previewViewmodel != null)
            {
                _previewViewmodel.PropertyChanged -= _onPreviewViewModelPropertyChanged;
                _previewViewmodel.Dispose();
            }
            _searchDirectory.MediaAdded -= _searchDirectory_MediaAdded;
            _searchDirectory.MediaRemoved -= _searchDirectory_MediaRemoved;
            _searchDirectory.MediaVerified -= _searchDirectory_MediaVerified;
            _itemsView.Filter -= _itemsFilter;
            _view.Closed -= _windowClosed;
            foreach (var item in _items)
                item.Dispose();
            Debug.WriteLine("MediaSearchViewModel disposed");
        }

        public Views.PreviewView PreviewView { get { return _previewViewmodel?.View; } }

        public double WindowWidth { get; set; }

        bool _canAddMediaToCollection(IMedia media, TMediaType requiredMediaType)
        {
            return
                media != null
                && media.MediaType == requiredMediaType
                &&
                   (requiredMediaType == TMediaType.Still && (_videoFormatDescription == null || media.VideoFormatDescription.SAR.Equals(_videoFormatDescription.SAR))
                 || media.MediaType == TMediaType.Movie && media.VideoFormatDescription.FrameRate.Equals(_frameRate)
                 || media.MediaType == TMediaType.Animation);
        }

        void _searchDirectory_MediaVerified(object sender, MediaEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)delegate()
            {
                _itemsView.Refresh();
            });
        }

        void _searchDirectory_MediaRemoved(object sender, MediaEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)delegate()
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

        void _searchDirectory_MediaAdded(object sender, MediaEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)delegate()
            {
                IMedia media = e.Media;
                if (media != null 
                    && _canAddMediaToCollection(media, _mediaType))
                    _items.Add(new MediaViewViewmodel(media));
            });
        }

        private readonly ObservableCollection<MediaViewViewmodel> _items;
        public ObservableCollection<MediaViewViewmodel> Items { get { return _items; } }
        public ICommand CommandAdd { get; private set; }
        
        private IEventClient _baseEvent;
        public IEventClient BaseEvent { get { return _baseEvent; }
            set
            {
                IEventClient b = _baseEvent;
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

        private bool _itemsFilter(object item)
        {
            MediaViewViewmodel mvm = item as MediaViewViewmodel;
            if (mvm == null || mvm.Media == null)
                return false;
            string mediaName = mvm.MediaName.ToLower();
            return mvm.MediaStatus == TMediaStatus.Available
                && (!(MediaCategory is TMediaCategory) || (MediaCategory as TMediaCategory?) == mvm.MediaCategory)
                && (_searchTextSplit.All(s => mediaName.Contains(s)));
        }

        public PreviewViewmodel PreviewViewmodel { get { return _previewViewmodel; } }

        private void _createCommands()
        {
            CommandAdd = new UICommand() { ExecuteDelegate = _add, CanExecuteDelegate = _allowAdd };
        }

        private string[] _searchTextSplit = new string[0];
        private string _searchText = string.Empty;
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

        static readonly IEnumerable<object> _mediaCategories = (new List<object>() { resources._all_ }).Concat(Enum.GetValues(typeof(TMediaCategory)).Cast<object>());
        public IEnumerable<object> MediaCategories { get { return _mediaCategories; } }

        private object _mediaCategory = null;
        public object MediaCategory
        {
            get { return _mediaCategory; }
            set {
                if (SetField(ref _mediaCategory, value, nameof(MediaCategory)))
                {
                    NotifyPropertyChanged(nameof(IsServerOrArchiveDirectory));
                    _itemsView.Refresh();
                }
            }
        }

        public IMedia SelectedMedia
        {
            get
            {
                var mediaVm = _selectedItem;
                if (mediaVm != null)
                    return mediaVm.Media;
                return null;
            }
        }

        private TimeSpan TCStart
        {
            get
            {
                if (_previewViewmodel != null)
                {
                    var pvlm = _previewViewmodel.LoadedMedia;
                    if (pvlm != null)
                    {
                        var s = _previewViewmodel.SelectedSegment;
                        if (s != null)
                            return s.TcIn;
                        else
                            if (_previewViewmodel.TcIn != pvlm.TcPlay || _previewViewmodel.TcOut != pvlm.TcPlay + pvlm.DurationPlay)
                                return _previewViewmodel.TcIn;
                    }
                }
                var mediaVm = SelectedItem;
                if (mediaVm != null)
                {
                    var segmentVM = mediaVm.SelectedSegment;
                    if (segmentVM == null)
                        return mediaVm.TcPlay;
                    else
                        return segmentVM.TcIn;
                }
                return TimeSpan.Zero;
            }
        }

        private TimeSpan Duration
        {
            get
            {
                if (_previewViewmodel != null)
                {
                    var pvlm = _previewViewmodel.LoadedMedia;
                    if (pvlm != null)
                    {
                        var s = _previewViewmodel.SelectedSegment;
                        if (s != null)
                            return s.Duration;
                        else
                            if (_previewViewmodel.TcIn != pvlm.TcPlay || _previewViewmodel.TcOut != pvlm.TcPlay + pvlm.DurationPlay)
                                return _previewViewmodel.DurationSelection;
                    }
                }
                var mediaVm = SelectedItem;
                if (mediaVm != null)
                {
                    var segmentVM = mediaVm.SelectedSegment;
                    if (segmentVM == null)
                        return mediaVm.DurationPlay;
                    else
                        return segmentVM.Duration;
                }
                return TimeSpan.Zero;
            }
        }


        private MediaViewViewmodel _selectedItem;
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
                        && ((IIngestDirectory)media.Directory).AccessType == TDirectoryAccessType.Direct
                        && !media.IsVerified)
                        media.ReVerify();
                    if (_previewViewmodel != null)
                        _previewViewmodel.Media = media;
                    InvalidateRequerySuggested();
                }
            } 
        }
        
        private string MediaName
        {
            get
            {
                if (_previewViewmodel != null)
                {
                    var pvlm = _previewViewmodel.LoadedMedia;
                    if (pvlm != null)
                    {
                        var s = _previewViewmodel.SelectedSegment;
                        if (s != null)
                            return pvlm.MediaName + " [" + s.SegmentName + "]";
                        else
                            if (_previewViewmodel.TcIn != pvlm.TcPlay || _previewViewmodel.TcOut != pvlm.TcPlay+pvlm.DurationPlay)
                                return pvlm.MediaName + " [fragment]";
                    }
                }
                var mediaVm = SelectedItem;
                if (mediaVm != null)
                {
                    var segmentVM = mediaVm.SelectedSegment;
                    if (segmentVM == null)
                        return mediaVm.MediaName;
                    else
                        return mediaVm.MediaName + " [" + segmentVM.SegmentName + "]";
                }
                return string.Empty;
            }
        }


        public TStartType NewEventStartType;

        private bool _allowAdd(object o)
        {
            IMedia sm = SelectedMedia;
            var be = _baseEvent;
            var previewVM = _previewViewmodel;
            return (sm != null)
                && (_mediaType != TMediaType.Movie || previewVM == null || previewVM.LoadedMedia == null || previewVM.LoadedMedia.MediaGuid == sm.MediaGuid) 
                && (be == null
                    || ((be.EventType == TEventType.Movie || be.EventType == TEventType.Live || be.EventType == TEventType.Rundown)
                        && _mediaType == TMediaType.Movie 
                        && ((NewEventStartType == TStartType.With && be.PlayState == TPlayState.Scheduled) 
                            || (NewEventStartType == TStartType.After && be.PlayState != TPlayState.Played && be.PlayState != TPlayState.Fading)))
                    || ((be.EventType == TEventType.Movie || be.EventType == TEventType.Live) 
                        && (_mediaType == TMediaType.Still || _mediaType == TMediaType.Animation)
                        && NewEventStartType == TStartType.With)
                    );
        }

        public bool IsMovie { get { return _mediaType == TMediaType.Movie; } }

        public bool IsServerOrArchiveDirectory
        {
            get
            {
                return (_mediaType == TMediaType.Movie) &&
                    !(_mediaCategory is TMediaCategory);
            }
        }

        public bool EnableCGElementsForNewEvents { get { return _engine.EnableCGElementsForNewEvents; } set { _engine.EnableCGElementsForNewEvents = value; } }
        public bool CanEnableCGElements { get { return _engine.CGElementsController != null && _mediaType == TMediaType.Movie; } } 
        
        private string _okButtonText = "OK";
        public string OkButtonText { get { return _okButtonText; }
            set {
                if (value != _okButtonText)
                {
                    _okButtonText = value;
                    NotifyPropertyChanged(nameof(OkButtonText));
                }
            }
        }

        private void _onBaseEventPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IEventClient.PlayState))
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
                handler(this, new MediaSearchEventArgs(sm.Media, sm.SelectedSegment == null ? null : sm.SelectedSegment.MediaSegment, MediaName, TCStart, Duration));
            if (_closeAfterAdd)
                _view.Close();
        }

        public void _windowClosed(object o, EventArgs e)
        {
            SearchWindowClosed?.Invoke(this, e);
        }


        public event EventHandler<MediaSearchEventArgs> MediaChoosen;
        public event EventHandler<EventArgs> SearchWindowClosed;

        public Action<MediaSearchEventArgs> ExecuteAction;

        public ICollectionView _itemsView { get; set; }
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
        public IMedia Media { get; private set; }
        public IMediaSegment MediaSegment {get; private set;}
        public string MediaName { get; private set; }
        public TimeSpan TCIn { get; private set; }
        public TimeSpan Duration { get; private set; }
    }
}
