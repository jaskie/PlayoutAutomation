using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Windows.Input;
using TAS.Client.Common;
using TAS.Common;

namespace TAS.Client.Config
{
    public class PlayoutServerViewmodel:EditViewmodelBase<Model.CasparServer>
    {
        protected override void OnDispose()
        {
            _playoutServerChannels.CollectionChanged -= _playoutServerChannels_CollectionChanged;
        }
        private string _serverAddress;
        private string _mediaFolder;
        private string _animationFolder;
        private TServerType _serverType;
        private PlayoutServerChannelViewmodel _selectedPlayoutServerChannel;
        static readonly Array _serverTypes = Enum.GetValues(typeof(TServerType));
        private readonly ObservableCollection<PlayoutServerChannelViewmodel> _playoutServerChannels;
        private PlayoutRecorderViewmodel _selectedPlayoutRecorder;
        private readonly ObservableCollection<PlayoutRecorderViewmodel> _playoutRecorders;
        public ICommand CommandAddChannel { get; private set; }
        public ICommand CommandDeleteChannel { get; private set; }
        public ICommand CommandAddRecorder { get; private set; }
        public ICommand CommandDeleteRecorder { get; private set; }
        private bool _isAnyChildChanged;

        public PlayoutServerViewmodel(Model.CasparServer playoutServer): base(playoutServer, new PlayoutServerView())
        {
            _playoutServerChannels = new ObservableCollection<PlayoutServerChannelViewmodel>(playoutServer.Channels.Select(p => new PlayoutServerChannelViewmodel(p)));
            _playoutServerChannels.CollectionChanged += _playoutServerChannels_CollectionChanged;
            CommandAddChannel = new UICommand() { ExecuteDelegate = _addChannel };
            CommandDeleteChannel = new UICommand() { ExecuteDelegate = _removeChannel, CanExecuteDelegate = o => _selectedPlayoutServerChannel != null };

            _playoutRecorders = new ObservableCollection<PlayoutRecorderViewmodel>(playoutServer.Recorders.Select(r => new PlayoutRecorderViewmodel(r)));
            _playoutRecorders.CollectionChanged += _playoutRecorders_CollectionChanged;
            CommandAddRecorder = new UICommand() { ExecuteDelegate = _addRecorder };
            CommandDeleteRecorder = new UICommand() { ExecuteDelegate = _removeRecorder, CanExecuteDelegate = o => _selectedPlayoutRecorder != null };
        }

        private void _removeRecorder(object obj)
        {
            _playoutRecorders.Remove(_selectedPlayoutRecorder);
        }

        private void _addRecorder(object obj)
        {
            _playoutRecorders.Add(new PlayoutRecorderViewmodel(new Config.Model.CasparRecorder { RecorderName = "New recorder" }));
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

        void _playoutServerChannels_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
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
            _playoutServerChannels.Add(newChannelVm);
            SelectedPlayoutServerChannel = newChannelVm;
        }

        private void _removeChannel(object o)
        {
            _playoutServerChannels.Remove(_selectedPlayoutServerChannel);
        }

        public override void ModelUpdate(object destObject = null)
        {
            foreach (var ch in _playoutServerChannels)
                ch.ModelUpdate();
            foreach (var r in _playoutRecorders)
                r.ModelUpdate();
            base.ModelUpdate(destObject);
        }

        public override bool IsModified { get { return  _isModified || _isAnyChildChanged || _playoutServerChannels.Any(c => c.IsModified) || _playoutRecorders.Any(r => r.IsModified); } }
        public bool IsRecordersVisible { get { return _serverType == TServerType.CasparTVP; } }

        public string ServerAddress { get { return _serverAddress; } set { SetField(ref _serverAddress, value, nameof(ServerAddress)); } }
        public string MediaFolder { get { return _mediaFolder; } set { SetField(ref _mediaFolder, value, nameof(MediaFolder)); } }
        public string AnimationFolder { get { return _animationFolder; } set { SetField(ref _animationFolder, value, nameof(AnimationFolder)); } }
        public TServerType ServerType
        {
            get { return _serverType; }
            set
            {
                if (SetField(ref _serverType, value, nameof(ServerType)))
                    NotifyPropertyChanged(nameof(IsRecordersVisible));
            }
        }
        public Array ServerTypes { get { return _serverTypes; } }

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
        public ObservableCollection<PlayoutServerChannelViewmodel> PlayoutServerChannels { get { return _playoutServerChannels; } }

        public ObservableCollection<PlayoutRecorderViewmodel> PlayoutServerRecorders { get { return _playoutRecorders; } }
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


    }
}
