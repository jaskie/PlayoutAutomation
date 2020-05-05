using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Input;
using TAS.Client.Common;
using TAS.Common;

namespace TAS.Client.Config.ViewModels.Playout
{
    public class PlayoutServerViewmodel : EditViewmodelBase<Model.CasparServer>
    {
        private string _serverAddress;
        private int _oscPort = 6250;
        private string _mediaFolder;
        private string _animationFolder;
        private TServerType _serverType;

        private PlayoutServerChannelViewmodel _selectedPlayoutServerChannel;
        private PlayoutRecorderViewmodel _selectedPlayoutRecorder;
        private bool _isAnyChildChanged;
        private TMovieContainerFormat _movieContainerFormat;
        private bool _isMediaFolderRecursive;

        public PlayoutServerViewmodel(Model.CasparServer playoutServer) : base(playoutServer)
        {
            PlayoutServerChannels = new ObservableCollection<PlayoutServerChannelViewmodel>(playoutServer.Channels.Select(p =>
                {
                    var newVm = new PlayoutServerChannelViewmodel(p);
                    return newVm;
                }));
            PlayoutServerChannels.CollectionChanged += _playoutServerChannels_CollectionChanged;
            CommandAddChannel = new UiCommand(_addChannel);
            CommandDeleteChannel = new UiCommand(_removeChannel, o => _selectedPlayoutServerChannel != null);

            PlayoutServerRecorders = new ObservableCollection<PlayoutRecorderViewmodel>(playoutServer.Recorders.Select(r =>
            {
                var newVm = new PlayoutRecorderViewmodel(r);
                return newVm;
            }));
            PlayoutServerRecorders.CollectionChanged += _playoutRecorders_CollectionChanged;
            CommandAddRecorder = new UiCommand(_addRecorder);
            CommandDeleteRecorder = new UiCommand(_removeRecorder, o => _selectedPlayoutRecorder != null);
        }


        public ICommand CommandAddChannel { get; }

        public ICommand CommandDeleteChannel { get; }

        public ICommand CommandAddRecorder { get; }

        public ICommand CommandDeleteRecorder { get; }

        public bool IsRecordersVisible => _serverType == TServerType.CasparTVP;

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

        public PlayoutServerChannelViewmodel SelectedPlayoutServerChannel
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

        public ObservableCollection<PlayoutServerChannelViewmodel> PlayoutServerChannels { get; }

        public ObservableCollection<PlayoutRecorderViewmodel> PlayoutServerRecorders { get; }

        public PlayoutRecorderViewmodel SelectedPlayoutRecorder
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

        protected override void Update(object destObject = null)
        {
            foreach (var ch in PlayoutServerChannels)
                ch.Save();
            foreach (var r in PlayoutServerRecorders)
                r.Save();
            base.Update(destObject);
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
            PlayoutServerRecorders.Add(new PlayoutRecorderViewmodel(new Model.CasparRecorder { RecorderName = "New recorder" }));
        }

        private void _playoutRecorders_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                Model.Recorders.Add(((PlayoutRecorderViewmodel)e.NewItems[0]).Model);
            }
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                Model.Recorders.Remove(((PlayoutRecorderViewmodel)e.OldItems[0]).Model);
            }
            _isAnyChildChanged = true;
        }

        private void _playoutServerChannels_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                Model.Channels.Add(((PlayoutServerChannelViewmodel)e.NewItems[0]).Model);
            }
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                Model.Channels.Remove(((PlayoutServerChannelViewmodel)e.OldItems[0]).Model);
            }
            _isAnyChildChanged = true;
        }

        private void _addChannel(object o)
        {
            var newChannelVm = new PlayoutServerChannelViewmodel(new Model.CasparServerChannel { Id = PlayoutServerChannels.Any() ? PlayoutServerChannels.Max(c => c.Id) + 1 : 1 });
            PlayoutServerChannels.Add(newChannelVm);
            SelectedPlayoutServerChannel = newChannelVm;
        }

        private void _removeChannel(object o)
        {
            PlayoutServerChannels.Remove(_selectedPlayoutServerChannel);
        }


        public void Save()
        {
            Update(Model);
        }
    }
}
