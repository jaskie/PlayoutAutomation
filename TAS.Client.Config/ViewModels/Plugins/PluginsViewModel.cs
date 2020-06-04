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
        private List<IPluginConfigurator> _plugins;

        private IPluginConfigurator _selectedPlugin;
        
        private IConfigEngine _engine;

        public PluginsViewModel(Engine engine)
        {
            _engine = engine;            
            _plugins = GetPlugins();
            
            Plugins = CollectionViewSource.GetDefaultView(_plugins);                       
        }        

        //Add available plugins based on Plugins folder
        private List<IPluginConfigurator> GetPlugins()
        {
            var plugins = new List<IPluginConfigurator>();            

            using (var catalog = new DirectoryCatalog(Path.Combine(Directory.GetCurrentDirectory(), "Plugins"), FileNameSearchPattern))
            {
                using (var container = new CompositionContainer(catalog))
                {
                    container.ComposeExportedValue("Engine", _engine);
                    plugins = container.GetExportedValues<IPluginConfigurator>().ToList();

                    foreach(var plugin in plugins)
                    {
                        plugin.RegisterUiTemplates();
                    }
                }
            }
                        
            return plugins;
        }              
        public ICollectionView Plugins { get; }
        public IPluginConfigurator SelectedPlugin { get => _selectedPlugin; set => SetField(ref _selectedPlugin, value); }

        public bool HasPlugins => _plugins.Count > 0 ? true : false;

        public void Save()
        {
            foreach (var plugin in _plugins)                
                plugin.Save();
        }

        protected override void OnDispose()
        {
            //
        }             
    }
}
