using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Server;
using System.Collections.ObjectModel;
using System.Windows;
using System.ComponentModel;
using System.Windows.Data;
using System.Diagnostics;
using TAS.Common;
using System.Windows.Input;

namespace TAS.Client.ViewModels
{
    public class MediaSearchViewmodel: ViewmodelBase
    {
        private readonly MediaManager _manager;
        private readonly PreviewViewmodel _previewViewmodel;
        private readonly TMediaType _mediaType;
        private readonly TVideoFormat? _videoFormat;
        private readonly Window _window;
        private readonly bool _closeAfterAdd;
        private MediaDirectory _searchDirectory;


        public MediaSearchViewmodel(EngineViewmodel engineVM, TMediaType mediaType, bool closeAfterAdd, TVideoFormat? videoFormat)
        {
            _manager = engineVM.Engine.MediaManager;
            _previewViewmodel = engineVM.PreviewViewmodel;
            if (_previewViewmodel != null)
                _previewViewmodel.PropertyChanged += OnPreviewPropertyChanged;
            _mediaType = mediaType;
            if (mediaType == TMediaType.AnimationFlash)
                _searchDirectory = _manager.AnimationDirectoryPGM;
            else
                _searchDirectory = _manager.MediaDirectoryPGM;
            _searchDirectory.MediaAdded += new EventHandler<MediaEventArgs>(_searchDirectory_MediaAdded);
            _searchDirectory.MediaRemoved += new EventHandler<MediaEventArgs>(_searchDirectory_MediaRemoved);
            _searchDirectory.MediaVerified += new EventHandler<MediaEventArgs>(_searchDirectory_MediaVerified);

            _videoFormat = videoFormat;
            _closeAfterAdd = closeAfterAdd;
            NewEventStartType = TStartType.After;
            if (!closeAfterAdd)
                OkButtonText = Properties.Resources._button_Add;
            _createCommands();
            _items = new ObservableCollection<MediaViewViewmodel>(_searchDirectory.Files
                .Where(m=> m.MediaType == mediaType && (videoFormat == null || m.VideoFormat == videoFormat))
                .Select(m => new MediaViewViewmodel(m)));
            _itemsView = CollectionViewSource.GetDefaultView(_items);
            _itemsView.SortDescriptions.Add(new SortDescription("MediaName", ListSortDirection.Ascending));
            _itemsView.Filter += _itemsFilter;
            
            _window = new MediaSearchView();
            _window.Owner = App.Current.MainWindow;
            _window.DataContext = this;
            _window.Closed += _windowClosed;
            _window.Show();
            //_window.Topmost = true;
        }

        ~MediaSearchViewmodel()
        {
            Debug.WriteLine("MediaSearch destroyed");
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
                    _itemsView.Refresh();
                }
            });
        }

        void _searchDirectory_MediaAdded(object sender, MediaEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)delegate()
            {
                if (e.Media != null && e.Media.MediaType == MediaType)
                    Items.Add(new MediaViewViewmodel(e.Media));
            });
        }

        private ICollectionView _itemsViewSource;
        private ObservableCollection<MediaViewViewmodel> _items;
        public ObservableCollection<MediaViewViewmodel> Items { get { return _items; } }
        public ICommand CommandAdd { get; private set; }
        
        private Event _baseEvent;
        public Event BaseEvent { get { return _baseEvent; }
            set
            {
                Event b = _baseEvent;
                if (b != value)
                {
                    if (b != null)
                        b.PropertyChanged -= _onBaseEventPropertyChanged;
                    _baseEvent = value;
                    NotifyPropertyChanged("CommandAdd");
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
            return mvm.MediaStatus == TMediaStatus.Available
                && (MediaCategory == null || MediaCategory == mvm.MediaCategory)
                && (string.IsNullOrWhiteSpace(SearchText) || mvm.MediaName.ToLower().Contains(SearchText.ToLower()));
        }

        public PreviewViewmodel PreviewViewmodel { get { return _previewViewmodel; } }

        private void _createCommands()
        {
            CommandAdd = new SimpleCommand() { ExecuteDelegate = _add, CanExecuteDelegate = _allowAdd };
        }

        private string _searchText;
        public string SearchText
        {
            get { return _searchText; }
            set
            {
                value = value.ToLower();
                if (value != _searchText)
                {
                    _searchText = value;
                    NotifyPropertyChanged("SearchText");
                    _itemsView.Refresh();
                }
            }
        }

        readonly Array _mediaCategories = Enum.GetValues(typeof(TMediaCategory));
        public Array MediaCategories { get { return _mediaCategories; } }

        public TMediaType MediaType { get { return _mediaType; } } 
        
        private TMediaCategory? _mediaCategory = null;
        public TMediaCategory? MediaCategory
        {
            get { return _mediaCategory; }
            set {
                if (_mediaCategory != value)
                {
                    _mediaCategory = value;
                    NotifyPropertyChanged("MediaCategory");
                    _itemsView.Refresh();
                }
            }
        }

        public Media SelectedMedia
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
                            return s.TCIn;
                        else
                            if (_previewViewmodel.TCIn != pvlm.TCPlay || _previewViewmodel.TCOut != pvlm.TCPlay + pvlm.DurationPlay)
                                return _previewViewmodel.TCIn;
                    }
                }
                var mediaVm = SelectedItem;
                if (mediaVm != null)
                {
                    var segmentVM = mediaVm.SelectedSegment;
                    if (segmentVM == null)
                        return mediaVm.TCPlay;
                    else
                        return segmentVM.TCIn;
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
                            if (_previewViewmodel.TCIn != pvlm.TCPlay || _previewViewmodel.TCOut != pvlm.TCPlay + pvlm.DurationPlay)
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
                    Media media = SelectedMedia;
                    if (media is IngestMedia
                        && ((IngestDirectory)media.Directory).AccessType == TDirectoryAccessType.Direct)
                        media.InvokeVerify();
                    if (_previewViewmodel != null)
                        _previewViewmodel.Media = media;
                    NotifyPropertyChanged("CommandAdd");
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
                            if (_previewViewmodel.TCIn != pvlm.TCPlay || _previewViewmodel.TCOut != pvlm.TCPlay+pvlm.DurationPlay)
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
            Media sm = SelectedMedia;
            var be = _baseEvent;
            var previewVM = _previewViewmodel;
            return (sm != null)
                && (MediaType != TMediaType.Movie || previewVM == null || previewVM.LoadedMedia == null || previewVM.LoadedMedia.MediaGuid == sm.MediaGuid) 
                && (be == null
                    || ((be.EventType == TEventType.Movie || be.EventType == TEventType.Live || be.EventType == TEventType.Rundown)
                        && MediaType == TMediaType.Movie 
                        && ((NewEventStartType == TStartType.With && be.PlayState == TPlayState.Scheduled) 
                            || (NewEventStartType == TStartType.After && be.PlayState != TPlayState.Played && be.PlayState != TPlayState.Fading)))
                    || ((be.EventType == TEventType.Movie || be.EventType == TEventType.Live) 
                        && (MediaType == TMediaType.Still || MediaType == TMediaType.AnimationFlash)
                        && NewEventStartType == TStartType.With)
                    );
        }

        public bool IsMovie { get { return _mediaType == TMediaType.Movie; } }

        private bool _filterMatch(Media media, string searchText)
        {
            if (string.IsNullOrEmpty(searchText))
                return true;
            string[] searchTexts = searchText.ToLower().Split(' ');
            string mediaName = media.MediaName.ToLower();
            string fileName = media.FileName.ToLower();
            return searchTexts.All(s =>
                string.IsNullOrEmpty(s)
                || mediaName.Contains(s)
                || fileName.Contains(s)
                );
        }

        private string _okButtonText = "OK";
        public string OkButtonText { get { return _okButtonText; }
            set {
                if (value != _okButtonText)
                {
                    _okButtonText = value;
                    NotifyPropertyChanged("OkButtonText");
                }
            }
        }

        private void _onBaseEventPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "PlayState")
                NotifyPropertyChanged("CommandAdd");
        }

        protected override void OnDispose()
        {
            BaseEvent = null;
            if (_previewViewmodel != null)
                _previewViewmodel.PropertyChanged -= OnPreviewPropertyChanged;
            _searchDirectory.MediaAdded -= new EventHandler<MediaEventArgs>(_searchDirectory_MediaAdded);
            _searchDirectory.MediaRemoved -= new EventHandler<MediaEventArgs>(_searchDirectory_MediaRemoved);
            _searchDirectory.MediaVerified -= new EventHandler<MediaEventArgs>(_searchDirectory_MediaVerified);
            Debug.WriteLine("MediaSearchViewModel disposed");
        }           

        private void OnPreviewPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "LoadedMedia")
                NotifyPropertyChanged("CommandAdd");
        }

        private  void _add(object param)
        {
            var handler = MediaChoosen;
            var sm = SelectedItem;
            if (handler!= null && sm != null)
                handler(this, new MediaSearchEventArgs(sm.Media, sm.SelectedSegment == null ? null : sm.SelectedSegment.MediaSegment, MediaName, TCStart, Duration));
            if (_closeAfterAdd)
                _window.Close();
        }

        public void _windowClosed(object o, EventArgs e)
        {
            if (SearchWindowClosed != null)
                SearchWindowClosed(this, e);
        }


        public event EventHandler<MediaSearchEventArgs> MediaChoosen;
        public event EventHandler<EventArgs> SearchWindowClosed;

        public Action<MediaSearchEventArgs> ExecuteAction;

        public ICollectionView _itemsView { get; set; }
    }

    public class MediaSearchEventArgs : EventArgs
    {
        public MediaSearchEventArgs(Media media, MediaSegment segment, string mediaName, TimeSpan tCIn, TimeSpan duration)
        {
            Media = media;
            MediaSegment = segment;
            MediaName = mediaName;
            TCIn = tCIn;
            Duration = duration;
        }
        public Media Media { get; private set; }
        public MediaSegment MediaSegment {get; private set;}
        public string MediaName { get; private set; }
        public TimeSpan TCIn { get; private set; }
        public TimeSpan Duration { get; private set; }
    }
}
