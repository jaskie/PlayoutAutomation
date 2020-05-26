using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Data;
using TAS.Client.Common;
using TAS.Client.Config.Model;
using TAS.Client.Config.Model.Plugins;
using TAS.Client.Config.ViewModels.Plugins.CgElementsController;

namespace TAS.Client.Config.ViewModels.Plugins
{
    public class PluginsViewModel : ViewModelBase
    {        
        private List<IPluginManager> _plugins;
        private IPluginManager _selectedPlugin;
        
        private Engine _engine;

        public PluginsViewModel(Engine engine)
        {
            _engine = engine;            
            _plugins = GetPlugins();
            
            Plugins = CollectionViewSource.GetDefaultView(_plugins);                       
        }        

        //Add available plugins based on Plugins folder
        private List<IPluginManager> GetPlugins()
        {
            var plugins = new List<IPluginManager>();
            var pluginNames = Directory.GetFiles("Plugins/", "TAS.Server.*.dll").Select(p => Path.GetFileNameWithoutExtension(p)).ToList();
            
            foreach(var name in pluginNames)
            {
                switch(name)
                {
                    case "TAS.Server.CgElementsController":
                        plugins.Add(new CgElementsControllerPluginManager(_engine));
                        break;
                }
            }

            return plugins;
        }              
        public ICollectionView Plugins { get; }
        public IPluginManager SelectedPlugin { get => _selectedPlugin; set => SetField(ref _selectedPlugin, value); }

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
