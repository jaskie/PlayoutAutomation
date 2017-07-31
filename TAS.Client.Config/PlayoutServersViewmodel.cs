using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Input;
using TAS.Client.Common;

namespace TAS.Client.Config
{
    public class PlayoutServersViewmodel: OkCancelViewmodelBase<Model.PlayoutServers>
    {
        private bool _isCollectionChanged;
        private PlayoutServerViewmodel _selectedServer;

        public PlayoutServersViewmodel(string connectionString, string connectionStringSecondary)
            : base(new Model.PlayoutServers(connectionString, connectionStringSecondary), typeof(PlayoutServersView), "Playout servers")
        {
            PlayoutServers = new ObservableCollection<PlayoutServerViewmodel>(Model.Servers.Select(s =>
                {
                    var vm = new PlayoutServerViewmodel(s);
                    vm.Load();
                    return vm;
                }));
            PlayoutServers.CollectionChanged += PlayoutServers_CollectionChanged;
            CommandAdd = new UICommand { ExecuteDelegate = Add };
            CommandDelete = new UICommand { ExecuteDelegate = o => PlayoutServers.Remove(_selectedServer), CanExecuteDelegate = o => _selectedServer != null };
        }

        public override bool IsModified { get { return _isCollectionChanged || PlayoutServers.Any(s => s.IsModified); } }

        public ICommand CommandAdd { get; }

        public ICommand CommandDelete { get; }

        public PlayoutServerViewmodel SelectedServer { get { return _selectedServer; } set { SetField(ref _selectedServer, value); } }

        public ObservableCollection<PlayoutServerViewmodel> PlayoutServers { get; }

        public override void Update(object destObject = null)
        {
            foreach (PlayoutServerViewmodel s in PlayoutServers)
                s.Update();
            Model.Save();
            base.Update(destObject);
        }

        protected override void OnDispose()
        {
            PlayoutServers.CollectionChanged -= PlayoutServers_CollectionChanged;
        }

        private void PlayoutServers_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
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

        private void Add(object obj)
        {
            var newPlayoutServer = new Model.CasparServer();
            Model.Servers.Add(newPlayoutServer);
            var newPlayoutServerViewmodel = new PlayoutServerViewmodel(newPlayoutServer);
            PlayoutServers.Add(newPlayoutServerViewmodel);
            SelectedServer = newPlayoutServerViewmodel;            
        }

    }
}
