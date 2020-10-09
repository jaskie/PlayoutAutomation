using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Windows.Data;
using TAS.Client.Common;
using TAS.Client.Common.Plugin;
using TAS.Client.Config.Model;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Configurator;

namespace TAS.Client.Config.ViewModels.Plugins
{
    public class PluginsViewModel : ViewModelBase
    {
        public static string FileNameSearchPattern { get; } = "TAS.Server.*.dll";
        private IConfigEngine _engine;
        public event EventHandler PluginChanged;

        private CgElementsControllersViewModel _cgElementsControllersViewModel;
        private VideoSwitchersViewModel _videoSwitchersViewModel;
        private GpisViewModel _gpisViewModel;
                
        private List<IPluginManager> _pluginManagers = new List<IPluginManager>();


        private IPluginManager _selectedPlugin;

        public PluginsViewModel(Engine engine)
        {
            _engine = engine;
            Plugins = CollectionViewSource.GetDefaultView(_pluginManagers);            
            Init();                                                
        }
        
        //Add available plugins based on Plugins folder
        private void Init()
        {
            if (ConfigurationPluginManager.Current.CgElementsControllers != null)
            {
                _cgElementsControllersViewModel = new CgElementsControllersViewModel(_engine);
                _cgElementsControllersViewModel.PluginChanged += OnPluginChanged;

                _pluginManagers.Add(_cgElementsControllersViewModel);
            }
            if (ConfigurationPluginManager.Current.Gpis != null)
            {
                _gpisViewModel = new GpisViewModel(_engine);
                _gpisViewModel.PluginChanged += OnPluginChanged;
                _pluginManagers.Add(_gpisViewModel);
            }
            if (ConfigurationPluginManager.Current.VideoSwitchers != null)
            {
                _videoSwitchersViewModel = new VideoSwitchersViewModel(_engine);
                _videoSwitchersViewModel.PluginChanged += OnPluginChanged;
                _pluginManagers.Add(_videoSwitchersViewModel);
            }
           
            Plugins.Refresh();
        }

        private void OnPluginChanged(object sender, EventArgs e)
        {
            PluginChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool HasPlugins => _pluginManagers.Count > 0;
        public ICollectionView Plugins { get; }                               
        public IPluginManager SelectedPlugin
        {
            get => _selectedPlugin;
            set
            {
                if (!SetField(ref _selectedPlugin, value))
                    return;                                
            }
        }
                                                           
        public void Save()
        {         
            if (_cgElementsControllersViewModel != null)
            {
                _cgElementsControllersViewModel.Save();
                _engine.CGElementsController = _cgElementsControllersViewModel.CgElementsController;
            }
            
            if (_videoSwitchersViewModel != null)
            {
                _videoSwitchersViewModel.Save();
                _engine.Router = _videoSwitchersViewModel.Router;
            }
            
            if (_gpisViewModel != null)
            {
                _gpisViewModel.Save();
                _engine.Gpis = _gpisViewModel.Gpis;
            }            
        }

        protected override void OnDispose()
        {
            if (_gpisViewModel != null)
                _gpisViewModel.PluginChanged -= OnPluginChanged;

            if (_videoSwitchersViewModel != null)
                _videoSwitchersViewModel.PluginChanged -= OnPluginChanged;

            if (_cgElementsControllersViewModel != null)
                _cgElementsControllersViewModel.PluginChanged -= OnPluginChanged;
        }             
    }
}
