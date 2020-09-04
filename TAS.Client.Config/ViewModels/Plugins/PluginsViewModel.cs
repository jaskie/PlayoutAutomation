using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Windows.Data;
using TAS.Client.Common;
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
        private RoutersViewModel _routersViewModel;
        private GpisViewModel _gpisViewModel;
                
        private List<IPluginManager> _plugins = new List<IPluginManager>();


        private IPluginManager _selectedPlugin;

        public PluginsViewModel(Engine engine)
        {
            _engine = engine;
            Plugins = CollectionViewSource.GetDefaultView(_plugins);            
            Init();                                                
        }
        
        //Add available plugins based on Plugins folder
        private void Init()
        {            
            using (var catalog = new DirectoryCatalog(Path.Combine(Directory.GetCurrentDirectory(), "Plugins"), FileNameSearchPattern))
            {
                using (var container = new CompositionContainer(catalog))
                {
                    container.ComposeExportedValue("Engine", _engine);                    
                    var pluginConfigurators = container.GetExportedValues<IPluginConfigurator>();                    
                    
                    foreach (var pluginConfigurator in pluginConfigurators)
                    {
                        if (pluginConfigurator.GetModel() is ICGElementsController && _cgElementsControllersViewModel == null)
                        {
                            _cgElementsControllersViewModel = new CgElementsControllersViewModel(_engine);
                            _cgElementsControllersViewModel.PluginChanged += OnPluginChanged;
                            _plugins.Add(_cgElementsControllersViewModel);
                        }

                        else if (pluginConfigurator.GetModel() is IVideoSwitch && _routersViewModel == null)
                        {
                            _routersViewModel = new RoutersViewModel(_engine);
                            _routersViewModel.PluginChanged += OnPluginChanged;
                            _plugins.Add(_routersViewModel);
                        }

                        else if (pluginConfigurator.GetModel() is IGpi && _gpisViewModel == null)
                        {
                            _gpisViewModel = new GpisViewModel(_engine);
                            _gpisViewModel.PluginChanged += OnPluginChanged;
                            _plugins.Add(_gpisViewModel);
                        }                            
                    }                                                      
                }
            }
            Plugins.Refresh();
        }

        private void OnPluginChanged(object sender, EventArgs e)
        {
            PluginChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool HasPlugins => _plugins.Count > 0;
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
            
            if (_routersViewModel != null)
            {
                _routersViewModel.Save();
                _engine.Router = _routersViewModel.Router;
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

            if (_routersViewModel != null)
                _routersViewModel.PluginChanged -= OnPluginChanged;

            if (_cgElementsControllersViewModel != null)
                _cgElementsControllersViewModel.PluginChanged -= OnPluginChanged;
        }             
    }
}
