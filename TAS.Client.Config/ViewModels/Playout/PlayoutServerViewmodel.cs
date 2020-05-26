using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Input;
using TAS.Client.Common;
using TAS.Client.Config.Model;
using TAS.Common;

namespace TAS.Client.Config.ViewModels.Playout
{
    public class PlayoutServerViewModel : OkCancelViewModelBase
    {
        private string _serverAddress;
        private int _oscPort = 6250;
        private string _mediaFolder;
        private string _animationFolder;
        private TServerType _serverType;

        private PlayoutServerChannelViewModel _selectedPlayoutServerChannel;
        private PlayoutRecorderViewModel _selectedPlayoutRecorder;
        private bool _isAnyChildChanged;
        private TMovieContainerFormat _movieContainerFormat;
        private bool _isMediaFolderRecursive;
        private Model.CasparServer _playoutServer;

        public PlayoutServerViewModel(Model.CasparServer playoutServer)
        {
            _playoutServer = playoutServer;
            PlayoutServerChannels = new ObservableCollection<PlayoutServerChannelViewModel>(playoutServer.Channels.Select(p =>
                {
                    var newVm = new PlayoutServerChannelViewModel(p);
                    return newVm;
                }));
            PlayoutServerChannels.CollectionChanged += _playoutServerChannels_CollectionChanged;
            CommandAddChannel = new UiCommand(_addChannel);
            CommandDeleteChannel = new UiCommand(_removeChannel, o => _selectedPlayoutServerChannel != null);

            PlayoutServerRecorders = new ObservableCollection<PlayoutRecorderViewModel>(playoutServer.Recorders.Select(r =>
            {
                var newVm = new PlayoutRecorderViewModel(r);
                return newVm;
            }));
            PlayoutServerRecorders.CollectionChanged += _playoutRecorders_CollectionChanged;
            CommandAddRecorder = new UiCommand(_addRecorder);
            CommandDeleteRecorder = new UiCommand(_removeRecorder, o => _selectedPlayoutRecorder != null);
            Init();
        }

        private void Init()
        {
            ServerAddress = _playoutServer.ServerAddress;
            OscPort = _playoutServer.OscPort;
            MediaFolder = _playoutServer.MediaFolder;
            IsMediaFolderRecursive = _playoutServer.IsMediaFolderRecursive;
            AnimationFolder = _playoutServer.AnimationFolder;
            ServerType = _playoutServer.ServerType;
            MovieContainerFormat = _playoutServer.MovieContainerFormat;
            IsModified = false;
        }

        public ICommand CommandAddChannel { get; }

        public ICommand CommandDeleteChannel { get; }

        public ICommand CommandAddRecorder { get; }

        public ICommand CommandDeleteRecorder { get; }

        public bool IsRecordersVisible => _serverType == TServerType.CasparTVP;

        public CasparServer PlayoutServer => _playoutServer;
        public string ServerAddress
        {
            get => _serverAddress;
            set => SetField(ref _serverAddress, value);
        }

        public int OscPort
        {
            get => _oscPort;
            set => SetField(ref _oscPort, value);
        }

        public string MediaFolder
        {
            get => _mediaFolder;
            set => SetField(ref _mediaFolder, value);
        }

        public bool IsMediaFolderRecursive
        {
            get => _isMediaFolderRecursive;
            set => SetField(ref _isMediaFolderRecursive, value);
        }


        public string AnimationFolder
        {
            get => _animationFolder;
            set => SetField(ref _animationFolder, value);
        }

        public TServerType ServerType
        {
            get => _serverType;
            set
            {
                if (SetField(ref _serverType, value))
                    NotifyPropertyChanged(nameof(IsRecordersVisible));
            }
        }

        public TMovieContainerFormat MovieContainerFormat { get => _movieContainerFormat; set => SetField(ref _movieContainerFormat, value); }

        public Array ServerTypes { get; } = Enum.GetValues(typeof(TServerType));

        public Array MovieContainerFormats { get; } = Enum.GetValues(typeof(TMovieContainerFormat));

        public PlayoutServerChannelViewModel SelectedPlayoutServerChannel
        {
            get => _selectedPlayoutServerChannel;
            set
            {
                if (_selectedPlayoutServerChannel == value)
                    return;
                _selectedPlayoutServerChannel = value;
                NotifyPropertyChanged();
            }
        }

        public ObservableCollection<PlayoutServerChannelViewModel> PlayoutServerChannels { get; }

        public ObservableCollection<PlayoutRecorderViewModel> PlayoutServerRecorders { get; }

        public PlayoutRecorderViewModel SelectedPlayoutRecorder
        {
            get => _selectedPlayoutRecorder;
            set
            {
                if (_selectedPlayoutRecorder == value)
                    return;
                _selectedPlayoutRecorder = value;
                NotifyPropertyChanged();
            }
        }       

        public override bool IsModified { get { return base.IsModified || _isAnyChildChanged || PlayoutServerChannels.Any(c => c.IsModified) || PlayoutServerRecorders.Any(r => r.IsModified); } }

        protected override void OnDispose()
        {
            PlayoutServerChannels.CollectionChanged -= _playoutServerChannels_CollectionChanged;
            PlayoutServerRecorders.CollectionChanged -= _playoutRecorders_CollectionChanged;
        }
        private void _removeRecorder(object obj)
        {
            PlayoutServerRecorders.Remove(_selectedPlayoutRecorder);
        }

        private void _addRecorder(object obj)
        {
            PlayoutServerRecorders.Add(new PlayoutRecorderViewModel(new Model.CasparRecorder { RecorderName = "New recorder" }));
        }

        private void _playoutRecorders_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                _playoutServer.Recorders.Add(((PlayoutRecorderViewModel)e.NewItems[0]).CasparRecorder);
            }
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                _playoutServer.Recorders.Remove(((PlayoutRecorderViewModel)e.OldItems[0]).CasparRecorder);
            }
            _isAnyChildChanged = true;
        }

        private void _playoutServerChannels_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                _playoutServer.Channels.Add(((PlayoutServerChannelViewModel)e.NewItems[0]).CasparServerChannel);
            }
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                _playoutServer.Channels.Remove(((PlayoutServerChannelViewModel)e.OldItems[0]).CasparServerChannel);
            }
            _isAnyChildChanged = true;
        }

        private void _addChannel(object o)
        {
            var newChannelVm = new PlayoutServerChannelViewModel(new Model.CasparServerChannel { Id = PlayoutServerChannels.Any() ? PlayoutServerChannels.Max(c => c.Id) + 1 : 1 });
            PlayoutServerChannels.Add(newChannelVm);
            SelectedPlayoutServerChannel = newChannelVm;
        }

        private void _removeChannel(object o)
        {
            PlayoutServerChannels.Remove(_selectedPlayoutServerChannel);
        }

        public override bool Ok(object obj = null)
        {
            foreach (var ch in PlayoutServerChannels)
                ch.Save();
            foreach (var r in PlayoutServerRecorders)
                r.Save();

            _playoutServer.AnimationFolder = AnimationFolder;
            _playoutServer.IsMediaFolderRecursive = IsMediaFolderRecursive;
            _playoutServer.MediaFolder = MediaFolder;
            _playoutServer.MovieContainerFormat = MovieContainerFormat;
            _playoutServer.OscPort = OscPort;
            _playoutServer.ServerAddress = ServerAddress;
            _playoutServer.ServerType = ServerType;            
            return true;
        }

        public void Save()
        {
            Ok();
        }
    }
}
