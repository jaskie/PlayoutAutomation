using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Input;
using TAS.Client.Common;

namespace TAS.Client.Config
{
    public class EnginesViewmodel: OkCancelViewmodelBase<Model.Engines>
    {

        private EngineViewmodel _selectedEngine;
        private bool _isCollectionCanged;
        
        public EnginesViewmodel(string connectionString, string connectionStringSecondary)
            : base(new Model.Engines(connectionString, connectionStringSecondary), typeof(EnginesView), "Engines")
        {
            Engines = new ObservableCollection<EngineViewmodel>(Model.EngineList.Select(e =>
            {
                var newVm = new EngineViewmodel(e);
                newVm.Load();
                return newVm;
            }));
            Engines.CollectionChanged += _engines_CollectionChanged;
            CommandAdd = new UICommand { ExecuteDelegate = _add };
            CommandDelete = new UICommand { ExecuteDelegate = o => Engines.Remove(_selectedEngine), CanExecuteDelegate = o => _selectedEngine != null };
        }
        
        public ICommand CommandAdd { get; }

        public ICommand CommandDelete { get; }

        public ObservableCollection<EngineViewmodel> Engines { get; }

        public EngineViewmodel SelectedEngine { get { return _selectedEngine; } set { SetField(ref _selectedEngine, value); } }

        public override void Update(object destObject = null)
        {
            foreach (var e in Engines)
                e.Update();
            Model.Save();
            base.Update(destObject);
        }

        public override bool IsModified
        {
            get
            {
                return _isCollectionCanged
                       || (Engines != null && Engines.Any(e => e.IsModified));
            }
        }

        protected override void OnDispose()
        {
            Engines.CollectionChanged -= _engines_CollectionChanged;
        }

        private void _engines_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                Model.EngineList.Add(((EngineViewmodel)e.NewItems[0]).Model);
            }
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                Model.EngineList.Remove(((EngineViewmodel)e.OldItems[0]).Model);
                Model.DeletedEngines.Add(((EngineViewmodel)e.OldItems[0]).Model);
            }
            _isCollectionCanged = true;
        }

        private void _add(object obj)
        {
            var newEngine = new Model.Engine() { Servers = Model.Servers, ArchiveDirectories = Model.ArchiveDirectories, VolumeReferenceLoudness = -23.0 };
            Model.EngineList.Add(newEngine);
            var newPlayoutServerViewmodel = new EngineViewmodel(newEngine);
            Engines.Add(newPlayoutServerViewmodel);
            SelectedEngine = newPlayoutServerViewmodel;            
        }

    }
}
