using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Windows.Input;
using TAS.Client.Common;
using TAS.Common;

namespace TAS.Client.Setup
{
    public class PlayoutServerViewmodel:EditViewmodelBase<Model.CasparServer>
    {
        protected override void OnDispose()
        {
            _playoutServerChannels.CollectionChanged -= _playoutServerChannels_CollectionChanged;
        }
        private string _serverAddress;
        private string _mediaFolder;
        private ulong _id;
        private TServerType _serverType;
        private PlayoutServerChannelViewmodel _selectedPlayoutServerChannel;
        static readonly Array _serverTypes = Enum.GetValues(typeof(TServerType));
        private readonly ObservableCollection<PlayoutServerChannelViewmodel> _playoutServerChannels;
        private readonly UICommand _commandAdd;
        private readonly UICommand _commandDelete;
        private bool _isCollectionChanged;
        public PlayoutServerViewmodel(Model.CasparServer playoutServer): base(playoutServer, new PlayoutServerView())
        {
            _playoutServerChannels = new ObservableCollection<PlayoutServerChannelViewmodel>(playoutServer.Channels.Select(p => new PlayoutServerChannelViewmodel(p)));
            _playoutServerChannels.CollectionChanged += _playoutServerChannels_CollectionChanged;
            _commandAdd = new UICommand() { ExecuteDelegate = _addChannel };
            _commandDelete = new UICommand() { ExecuteDelegate = _removeChannel, CanExecuteDelegate = o => _selectedPlayoutServerChannel != null };
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
            _isCollectionChanged = true;
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

        public override void Save(object destObject = null)
        {
            foreach (PlayoutServerChannelViewmodel ch in _playoutServerChannels)
                ch.Save();
            base.Save(destObject);
        }

        public override bool Modified { get { return _isCollectionChanged || _playoutServerChannels.Any(c => c.Modified); } }

        public string ServerAddress { get { return _serverAddress; } set { SetField(ref _serverAddress, value, "ServerAddress"); } }
        public string MediaFolder { get { return _mediaFolder; } set { SetField(ref _mediaFolder, value, "MediaFolder"); } }
        public ulong Id { get { return _id; }}
        public TServerType ServerType { get { return _serverType; } set { SetField(ref _serverType, value, "ServerType"); } }
        public Array ServerTypes { get { return _serverTypes; } }

        public PlayoutServerChannelViewmodel SelectedPlayoutServerChannel { get { return _selectedPlayoutServerChannel; } set { SetField(ref _selectedPlayoutServerChannel, value, "SelectedPlayoutServerChannel"); } }
        public ObservableCollection<PlayoutServerChannelViewmodel> PlayoutServerChannels { get { return _playoutServerChannels; } }
        public ICommand CommandAdd { get { return _commandAdd; } }
        public ICommand CommandDelete { get { return _commandDelete; } }

    }
}
