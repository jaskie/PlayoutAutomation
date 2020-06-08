using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Windows.Data;
using TAS.Client.Common;
using TAS.Client.Config.Model;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Configurator;

namespace TAS.Client.Config.ViewModels.Plugins
{
    public class PluginsViewModel : ViewModelBase
    {
        private const string FileNameSearchPattern = "TAS.Server.*.dll";
        
        [ImportMany(typeof(IPluginConfigurator))]
        private List<IPluginConfigurator> _pluginConfigurators;

        private IPluginConfigurator _selectedPluginConfigurator;
        
        private IConfigEngine _engine;

        public PluginsViewModel(Engine engine)
        {
            _engine = engine;
            Init();
            
            PluginConfigurators = CollectionViewSource.GetDefaultView(_pluginConfigurators);                       
        }

        //Add available plugins based on Plugins folder
        private void Init()
        {            
            using (var catalog = new DirectoryCatalog(Path.Combine(Directory.GetCurrentDirectory(), "Plugins"), FileNameSearchPattern))
            {
                using (var container = new CompositionContainer(catalog))
                {
                    container.ComposeExportedValue("Engine", _engine);                    
                    _pluginConfigurators = container.GetExportedValues<IPluginConfigurator>().ToList();

                    foreach (var pluginConfigurator in _pluginConfigurators)
                    {
                        pluginConfigurator.Initialize();
                    }
                }
            }
        }
                
        public ICollectionView PluginConfigurators { get; }
        
        public IPluginConfigurator SelectedPluginConfigurator { get => _selectedPluginConfigurator; set => SetField(ref _selectedPluginConfigurator, value); }

        public bool HasPlugins => _pluginConfigurators.Count > 0 ? true : false;

        public void Save()
        {
            foreach (var pluginConfigurator in _pluginConfigurators)                
                pluginConfigurator.Save();
        }

        protected override void OnDispose()
        {
            //
        }             
    }
}
