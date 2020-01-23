using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Windows.Input;
using TAS.Client.Common;
using TAS.Common;

namespace TAS.Client.Config
{
    public class EnginesViewmodel: OkCancelViewmodelBase<Model.Engines>
    {

        private EngineViewmodel _selectedEngine;
        private bool _isCollectionCanged;
        
        public EnginesViewmodel(DatabaseType databaseType, ConnectionStringSettingsCollection connectionStringSettingsCollection)
            : base(new Model.Engines(databaseType, connectionStringSettingsCollection), typeof(EnginesView), "Engines")
        {
            Engines = new ObservableCollection<EngineViewmodel>(Model.EngineList.Select(e => new EngineViewmodel(e)));
            Engines.CollectionChanged += _engines_CollectionChanged;
            CommandAdd = new UiCommand(_add);
            CommandDelete = new UiCommand(o => Engines.Remove(_selectedEngine), o => _selectedEngine != null);
        }
        
        public ICommand CommandAdd { get; }

        public ICommand CommandDelete { get; }

        public ObservableCollection<EngineViewmodel> Engines { get; }

        public EngineViewmodel SelectedEngine
        {
            get => _selectedEngine;
            set
            {
                if (_selectedEngine == value)
                    return;
                _selectedEngine = value;
                NotifyPropertyChanged();
            }
        }

        protected override void Update(object destObject = null)
        {
            foreach (var e in Engines)
                e.Save();
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
            Model.Dispose();
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
            var newEngine = new Model.Engine() { Servers = Model.Servers, ArchiveDirectories = Model.ArchiveDirectories };
            Model.EngineList.Add(newEngine);
            var newPlayoutServerViewmodel = new EngineViewmodel(newEngine);
            Engines.Add(newPlayoutServerViewmodel);
            SelectedEngine = newPlayoutServerViewmodel;            
        }

    }
}
