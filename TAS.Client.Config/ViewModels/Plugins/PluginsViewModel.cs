using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Xml.Serialization;
using TAS.Client.Common;
using TAS.Client.Config.Model;
using TAS.Client.Config.Model.Plugins;
using TAS.Client.Config.ViewModels.Plugins.CgElementsController;
using TAS.Common;

namespace TAS.Client.Config.ViewModels.Plugins
{
    public class PluginsViewModel : ViewModelBase
    {        
        private List<IPluginManager> _plugins;
        private IPluginManager _selectedPlugin;

        private Model.Engines _engines;
        private Engine _selectedEngine;

        public PluginsViewModel(DatabaseType databaseType, ConnectionStringSettingsCollection connectionStringSettingsCollection)
        {           
            _engines = new Model.Engines(databaseType, connectionStringSettingsCollection);
            _plugins = GetPlugins();
            
            Plugins = CollectionViewSource.GetDefaultView(_plugins);
            Engines = CollectionViewSource.GetDefaultView(_engines.EngineList);            
        }        

        //Add available plugins based on added to Plugins folder
        private List<IPluginManager> GetPlugins()
        {
            var plugins = new List<IPluginManager>();
            var pluginNames = Directory.GetFiles("Plugins/", "TAS.Server.*.dll").Select(p => Path.GetFileNameWithoutExtension(p)).ToList();
            
            foreach(var name in pluginNames)
            {
                switch(name)
                {
                    case "TAS.Server.CgElementsController":
                        plugins.Add(new CgElementsControllerPluginManager(_engines));
                        break;
                }
            }

            return plugins;
        }
        
        public ICollectionView Engines { get; }
        public ICollectionView Plugins { get; }
        public IPluginManager SelectedPlugin { get => _selectedPlugin; set => SetField(ref _selectedPlugin, value); }
        public Engine SelectedEngine { get => _selectedEngine; set => SetField(ref _selectedEngine, value); }
        protected override void OnDispose()
        {
            //
        }
    }
}
