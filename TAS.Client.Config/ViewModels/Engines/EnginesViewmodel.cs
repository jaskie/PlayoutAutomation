using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Windows.Documents;
using System.Windows.Input;
using TAS.Client.Common;
using TAS.Client.Config.Model;
using TAS.Common;
using TAS.Common.Interfaces.Configurator;

namespace TAS.Client.Config.ViewModels.Engines
{
    public class EnginesViewModel : OkCancelViewModelBase
    {        
        private EngineViewModel _selectedEngine;
        private bool _isCollectionCanged;

        private Model.Engines _engines;
        
        public EnginesViewModel(DatabaseType databaseType, ConnectionStringSettingsCollection connectionStringSettingsCollection)
        {
            _engines = new Model.Engines(databaseType, connectionStringSettingsCollection);
            Engines = new ObservableCollection<EngineViewModel>(_engines.EngineList.Select(e => new EngineViewModel(e)));
            Engines.CollectionChanged += _engines_CollectionChanged;
            CommandAdd = new UiCommand(_add);
            CommandDelete = new UiCommand(o => Engines.Remove(_selectedEngine), o => _selectedEngine != null);
        }        
        
        public ICommand CommandAdd { get; }

        public ICommand CommandDelete { get; }

        public ObservableCollection<EngineViewModel> Engines { get; }

        public EngineViewModel SelectedEngine
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

        private void Update()
        {
            foreach (var e in Engines)
                e.Save();
            
            _engines.Save();            
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
            _engines.Dispose();
        }

        private void _engines_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                _engines.EngineList.Add(((EngineViewModel)e.NewItems[0]).Engine);
            }
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                _engines.EngineList.Remove(((EngineViewModel)e.OldItems[0]).Engine);
                _engines.DeletedEngines.Add(((EngineViewModel)e.OldItems[0]).Engine);
            }
            _isCollectionCanged = true;
        }

        private void _add(object obj)
        {
            //var serversList = new List<IConfigCasparServer>();
            //foreach(var server in _engines.Servers)
            //{
            //    serversList.Add(server);
            //}

            var newEngine = new Model.Engine() 
            {                 
                Servers = _engines.Servers.ToList(), 
                ArchiveDirectories = _engines.ArchiveDirectories 
            };
            _engines.EngineList.Add(newEngine);
            var newPlayoutServerViewmodel = new EngineViewModel(newEngine);
            Engines.Add(newPlayoutServerViewmodel);
            SelectedEngine = newPlayoutServerViewmodel;            
        }

        public override bool Ok(object obj)
        {
            Update();
            return true;
        }        
    }
}
