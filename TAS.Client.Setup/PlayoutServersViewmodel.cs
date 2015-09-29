using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Input;
using TAS.Client.Common;
using TAS.Server.Interfaces;

namespace TAS.Client.Setup
{
    public class PlayoutServersViewmodel: OkCancelViewmodelBase<Model.PlayoutServers>
    {
        private UICommand _commandAdd;
        private UICommand _commandDelete;
        protected override void OnDispose() { }
        public PlayoutServersViewmodel(string connectionString): base(new Model.PlayoutServers(connectionString), new PlayoutServersView(), "Playout servers")
        {
            _commandAdd = new UICommand() { ExecuteDelegate = _add };
            _commandDelete = new UICommand() { ExecuteDelegate = _delete };
            _playoutServers = new ObservableCollection<PlayoutServerViewmodel>(Model.PlayoutServerList.Select(s => new PlayoutServerViewmodel(s)));
        }

        private void _delete(object obj)
        {
            throw new NotImplementedException();
        }

        private void _add(object obj)
        {
            throw new NotImplementedException();
        }

        public ICommand CommandAdd { get { return _commandAdd; } }
        public ICommand CommandDelete { get { return _commandDelete; } }
        PlayoutServerViewmodel _selectedServer;
        public PlayoutServerViewmodel SelectedServer { get { return _selectedServer; } set { SetField(ref _selectedServer, value, "SelectedServer"); } }
        readonly ObservableCollection<PlayoutServerViewmodel> _playoutServers;
        public ObservableCollection<PlayoutServerViewmodel> PlayoutServers { get { return _playoutServers; } }
    }
}
