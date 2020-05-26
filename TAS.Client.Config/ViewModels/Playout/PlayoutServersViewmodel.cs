using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Windows.Input;
using TAS.Client.Common;
using TAS.Common;

namespace TAS.Client.Config.ViewModels.Playout
{
    public class PlayoutServersViewmodel : OkCancelViewModelBase
    {
        private bool _isCollectionChanged;
        private PlayoutServerViewmodel _selectedServer;
        private Model.PlayoutServers _playoutServers;
        public PlayoutServersViewmodel(DatabaseType databaseType, ConnectionStringSettingsCollection connectionStringSettingsCollection)
        {
            _playoutServers = new Model.PlayoutServers(databaseType, connectionStringSettingsCollection);
            PlayoutServers = new ObservableCollection<PlayoutServerViewmodel>(_playoutServers.Servers.Select(s => new PlayoutServerViewmodel(s)));
            PlayoutServers.CollectionChanged += PlayoutServers_CollectionChanged;
            CommandAdd = new UiCommand(Add);
            CommandDelete = new UiCommand(o => PlayoutServers.Remove(_selectedServer), o => _selectedServer != null);
        }

        public override bool IsModified { get { return _isCollectionChanged || PlayoutServers.Any(s => s.IsModified); } }

        public ICommand CommandAdd { get; }

        public ICommand CommandDelete { get; }

        public PlayoutServerViewmodel SelectedServer
        {
            get => _selectedServer;
            set
            {
                if (_selectedServer == value)
                    return;
                _selectedServer = value;
                NotifyPropertyChanged();
            }
        }

        public ObservableCollection<PlayoutServerViewmodel> PlayoutServers { get; }
        
        protected override void OnDispose()
        {
            PlayoutServers.CollectionChanged -= PlayoutServers_CollectionChanged;
            _playoutServers.Dispose();
        }

        private void PlayoutServers_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                _playoutServers.Servers.Add(((PlayoutServerViewmodel)e.NewItems[0]).PlayoutServer);
            }
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                _playoutServers.Servers.Remove(((PlayoutServerViewmodel)e.OldItems[0]).PlayoutServer);
                _playoutServers.DeletedServers.Add(((PlayoutServerViewmodel)e.OldItems[0]).PlayoutServer);
            }
            _isCollectionChanged = true;
        }

        private void Add(object obj)
        {
            var newPlayoutServer = new Model.CasparServer();
            _playoutServers.Servers.Add(newPlayoutServer);
            var newPlayoutServerViewmodel = new PlayoutServerViewmodel(newPlayoutServer);
            PlayoutServers.Add(newPlayoutServerViewmodel);
            SelectedServer = newPlayoutServerViewmodel;            
        }

        public override bool Ok(object obj = null)
        {
            foreach (PlayoutServerViewmodel s in PlayoutServers)
                s.Save();
            _playoutServers.Save();
            return true;
        }       
    }
}
