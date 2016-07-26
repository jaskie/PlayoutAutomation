using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Windows.Input;
using TAS.Client.Common;
using TAS.Server.Interfaces;

namespace TAS.Client.Config
{
    public class PlayoutServersViewmodel: OkCancelViewmodelBase<Model.PlayoutServers>
    {
        readonly UICommand _commandAdd;
        readonly UICommand _commandDelete;
        protected override void OnDispose()
        {
            _playoutServers.CollectionChanged -= _playoutServers_CollectionChanged;
        }
        public PlayoutServersViewmodel(string connectionString, string connectionStringSecondary)
            : base(new Model.PlayoutServers(connectionString, connectionStringSecondary), new PlayoutServersView(), "Playout servers")
        {
            _commandAdd = new UICommand() { ExecuteDelegate = _add };
            _commandDelete = new UICommand() { ExecuteDelegate = o => _playoutServers.Remove(_selectedServer), CanExecuteDelegate = o => _selectedServer != null };
            _playoutServers = new ObservableCollection<PlayoutServerViewmodel>(Model.Servers.Select(s => new PlayoutServerViewmodel(s)));
            _playoutServers.CollectionChanged += _playoutServers_CollectionChanged;
        }

        void _playoutServers_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                Model.Servers.Add(((PlayoutServerViewmodel)e.NewItems[0]).Model);
            }
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                Model.Servers.Remove(((PlayoutServerViewmodel)e.OldItems[0]).Model);
                Model.DeletedServers.Add(((PlayoutServerViewmodel)e.OldItems[0]).Model);
            }
            _isCollectionChanged = true;
        }

        private void _add(object obj)
        {
            var newPlayoutServer = new Model.CasparServer();
            Model.Servers.Add(newPlayoutServer);
            var newPlayoutServerViewmodel = new PlayoutServerViewmodel(newPlayoutServer);
            _playoutServers.Add(newPlayoutServerViewmodel);
            SelectedServer = newPlayoutServerViewmodel;            
        }
        public override void ModelUpdate(object destObject = null)
        {
            foreach (PlayoutServerViewmodel s in _playoutServers)
                s.ModelUpdate();
            Model.Save();
            base.ModelUpdate(destObject);
        }

        private bool _isCollectionChanged;
        public override bool IsModified { get { return _isCollectionChanged || _playoutServers.Any(s => s.IsModified); } }


        public ICommand CommandAdd { get { return _commandAdd; } }
        public ICommand CommandDelete { get { return _commandDelete; } }
        PlayoutServerViewmodel _selectedServer;
        public PlayoutServerViewmodel SelectedServer { get { return _selectedServer; } set { SetField(ref _selectedServer, value, nameof(SelectedServer)); } }
        readonly ObservableCollection<PlayoutServerViewmodel> _playoutServers;
        public ObservableCollection<PlayoutServerViewmodel> PlayoutServers { get { return _playoutServers; } }
    }
}
