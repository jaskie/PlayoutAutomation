using MySqlX.XDevAPI;
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
                
        private List<IPluginConfigurator> _pluginConfigurators = new List<IPluginConfigurator>();
        private List<IPluginConfigurator> _cgElementsControllerConfigurators = new List<IPluginConfigurator>();
        private List<IPluginConfigurator> _routerConfigurators = new List<IPluginConfigurator>();

        private IPluginConfigurator _selectedPluginConfigurator;
        private IPluginConfigurator _selectedCgElementsControllerConfigurator;
        private IPluginConfigurator _selectedRouterConfigurator;

        private bool? _isCgElementsControllerEnabled;
        private bool? _isRouterEnabled;

        private IConfigEngine _engine;

        public PluginsViewModel(Engine engine)
        {
            _engine = engine;
            LoadCommands();
            Init();
            
            PluginConfigurators = CollectionViewSource.GetDefaultView(_pluginConfigurators);
            CgElementsControllerConfigurators = CollectionViewSource.GetDefaultView(_cgElementsControllerConfigurators);
            RouterConfigurators = CollectionViewSource.GetDefaultView(_routerConfigurators);
        }

        private void LoadCommands()
        {
            EditRouterCommand = new UiCommand(EditRouterSettings, CanEditRouterSettings);
            EditCgElementsControllerCommand = new UiCommand(EditCgElementsController, CanEditCgElementsController);
        }

        private bool CanEditCgElementsController(object obj)
        {
            if (_selectedCgElementsControllerConfigurator != null)
                return true;
            return false;
        }

        private bool CanEditRouterSettings(object obj)
        {
            if (_selectedRouterConfigurator != null)
                return true;
            return false;
        }

        private void EditCgElementsController(object obj)
        {
            SelectedPluginConfigurator = null; //this will deselect datagrid
            SelectedPluginConfigurator = _selectedCgElementsControllerConfigurator;
        }

        private void EditRouterSettings(object obj)
        {
            SelectedPluginConfigurator = null; //this will deselect datagrid
            SelectedPluginConfigurator = _selectedRouterConfigurator;
        }

        //Add available plugins based on Plugins folder
        private void Init()
        {            
            using (var catalog = new DirectoryCatalog(Path.Combine(Directory.GetCurrentDirectory(), "Plugins"), FileNameSearchPattern))
            {
                using (var container = new CompositionContainer(catalog))
                {
                    container.ComposeExportedValue("Engine", _engine);                    
                    var pluginConfigurators = container.GetExportedValues<IPluginConfigurator>().ToList();

                    foreach (var pluginConfigurator in pluginConfigurators)
                    {                        
                        if (pluginConfigurator.GetModel() is ICGElementsController)
                        {
                            pluginConfigurator.Initialize(_engine.CGElementsController);
                            _cgElementsControllerConfigurators.Add(pluginConfigurator);
                        }
                            
                        //else if (pluginConfigurator.GetModel() is IRouter)
                        //{
                        //    pluginConfigurator.Initialize(_engine.Router);
                        //    _routerConfigurators.Add(pluginConfigurator);
                        //}
                            
                        else
                        {
                            pluginConfigurator.Initialize(_engine.Plugins.FirstOrDefault(p => p.GetType() == pluginConfigurator.GetModel().GetType()));
                            _pluginConfigurators.Add(pluginConfigurator);
                        }                                                    
                    }

                    //set cg and router comboboxes/checkboxes
                    SelectedCgElementsControllerConfigurator = _cgElementsControllerConfigurators.FirstOrDefault(p => p.GetModel()?.GetType() == _engine.CGElementsController?.GetType());                    
                    IsCgElementsControllerEnabled = _engine.CGElementsController?.IsEnabled ?? false;

                    SelectedRouterConfigurator = _routerConfigurators.FirstOrDefault(p => p.GetModel()?.GetType() == _engine.CGElementsController?.GetType());
                    if (_selectedPluginConfigurator != null)
                        IsRouterEnabled = _selectedRouterConfigurator.IsEnabled;
                    else
                        IsRouterEnabled = false;
                }
            }
        }
                
        public ICollectionView PluginConfigurators { get; }
        public ICollectionView CgElementsControllerConfigurators { get; }
        public ICollectionView RouterConfigurators { get; }        
        public UiCommand EditRouterCommand { get; private set; }
        public UiCommand EditCgElementsControllerCommand { get; private set; }

        public IPluginConfigurator SelectedPluginConfigurator
        {
            get => _selectedPluginConfigurator;
            set
            {
                if (!SetField(ref _selectedPluginConfigurator, value))
                    return;                                
            }
        }

        public bool HasPlugins => _pluginConfigurators.Count > 0 ? true : false;

        public IPluginConfigurator SelectedCgElementsControllerConfigurator 
        { 
            get => _selectedCgElementsControllerConfigurator;
            set
            {
                if (!SetField(ref _selectedCgElementsControllerConfigurator, value))
                    return;

                _selectedCgElementsControllerConfigurator.IsEnabled = _isCgElementsControllerEnabled ?? false;
            }
        }

        public IPluginConfigurator SelectedRouterConfigurator 
        { 
            get => _selectedRouterConfigurator; 
            set => SetField(ref _selectedRouterConfigurator, value); 
        }
        public bool? IsCgElementsControllerEnabled 
        { 
            get => _isCgElementsControllerEnabled;
            set
            {
                if (!SetField(ref _isCgElementsControllerEnabled, value))
                    return;

                if (value == null)
                    return;

                if (_selectedCgElementsControllerConfigurator != null)
                    _selectedCgElementsControllerConfigurator.IsEnabled = (bool)value;
            }
        }
        public bool? IsRouterEnabled { get => _isRouterEnabled; set => SetField(ref _isRouterEnabled, value); }
        public bool HasCgControllers => _cgElementsControllerConfigurators.Count > 0 ? true : false;
        public bool HasRouters => _routerConfigurators.Count > 0 ? true : false;

        public void Save()
        {
            foreach (var pluginConfigurator in _pluginConfigurators)
            {
                pluginConfigurator.Save();
                _engine.Plugins.Add((IPlugin)pluginConfigurator.GetModel());
            }

            _selectedCgElementsControllerConfigurator.Save();
            _engine.CGElementsController = (ICGElementsController)_selectedCgElementsControllerConfigurator.GetModel();            
        }

        protected override void OnDispose()
        {
            //
        }             
    }
}
