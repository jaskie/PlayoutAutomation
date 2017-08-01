using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Input;
using TAS.Client.Common;
using TAS.Server.Common;

namespace TAS.Client.Config
{
    public class PlayoutServerViewmodel:EditViewmodelBase<Model.CasparServer>
    {
        private string _serverAddress;
        private int _oscPort = 6250;
        private string _mediaFolder;
        private string _animationFolder;
        private TServerType _serverType;
        private PlayoutServerChannelViewmodel _selectedPlayoutServerChannel;
        private PlayoutRecorderViewmodel _selectedPlayoutRecorder;
        private bool _isAnyChildChanged;

        public PlayoutServerViewmodel(Model.CasparServer playoutServer): base(playoutServer)
        {
            PlayoutServerChannels = new ObservableCollection<PlayoutServerChannelViewmodel>(playoutServer.Channels.Select(p =>
                {
                    var newVm = new PlayoutServerChannelViewmodel(p);
                    newVm.Load();
                    return newVm;
                }));
            PlayoutServerChannels.CollectionChanged += _playoutServerChannels_CollectionChanged;
            CommandAddChannel = new UICommand { ExecuteDelegate = _addChannel };
            CommandDeleteChannel = new UICommand { ExecuteDelegate = _removeChannel, CanExecuteDelegate = o => _selectedPlayoutServerChannel != null };

            PlayoutServerRecorders = new ObservableCollection<PlayoutRecorderViewmodel>(playoutServer.Recorders.Select(r =>
            {
                var newVm = new PlayoutRecorderViewmodel(r);
                newVm.Load();
                return newVm;
            }));
            PlayoutServerRecorders.CollectionChanged += _playoutRecorders_CollectionChanged;
            CommandAddRecorder = new UICommand { ExecuteDelegate = _addRecorder };
            CommandDeleteRecorder = new UICommand { ExecuteDelegate = _removeRecorder, CanExecuteDelegate = o => _selectedPlayoutRecorder != null };
        }


        public ICommand CommandAddChannel { get; }

        public ICommand CommandDeleteChannel { get; }

        public ICommand CommandAddRecorder { get; }

        public ICommand CommandDeleteRecorder { get; }
        
        public bool IsRecordersVisible => _serverType == TServerType.CasparTVP;

        public string ServerAddress { get { return _serverAddress; } set { SetField(ref _serverAddress, value); } }

        public int OscPort { get { return _oscPort; } set{ SetField(ref _oscPort, value); } }

        public string MediaFolder { get { return _mediaFolder; } set { SetField(ref _mediaFolder, value); } }

        public string AnimationFolder { get { return _animationFolder; } set { SetField(ref _animationFolder, value); } }

        public TServerType ServerType
        {
            get { return _serverType; }
            set
            {
                if (SetField(ref _serverType, value))
                    NotifyPropertyChanged(nameof(IsRecordersVisible));
            }
        }

        public Array ServerTypes { get; } = Enum.GetValues(typeof(TServerType));

        public PlayoutServerChannelViewmodel SelectedPlayoutServerChannel
        {
            get { return _selectedPlayoutServerChannel; }
            set
            {
                if (_selectedPlayoutServerChannel != value)
                {
                    _selectedPlayoutServerChannel = value;
                    NotifyPropertyChanged(nameof(SelectedPlayoutServerChannel));
                }
            }
        }

        public ObservableCollection<PlayoutServerChannelViewmodel> PlayoutServerChannels { get; }

        public ObservableCollection<PlayoutRecorderViewmodel> PlayoutServerRecorders { get; }

        public PlayoutRecorderViewmodel SelectedPlayoutRecorder
        {
            get { return _selectedPlayoutRecorder; }
            set
            {
                if (_selectedPlayoutRecorder != value)
                {
                    _selectedPlayoutRecorder = value;
                    NotifyPropertyChanged(nameof(SelectedPlayoutRecorder));
                }
            }
        }

        public override void Update(object destObject = null)
        {
            foreach (var ch in PlayoutServerChannels)
                ch.Update();
            foreach (var r in PlayoutServerRecorders)
                r.Update();
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
            var newChannelVm = new PlayoutServerChannelViewmodel(new Model.CasparServerChannel());
            PlayoutServerChannels.Add(newChannelVm);
            SelectedPlayoutServerChannel = newChannelVm;
        }

        private void _removeChannel(object o)
        {
            PlayoutServerChannels.Remove(_selectedPlayoutServerChannel);
        }


    }
}
