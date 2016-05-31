using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using TAS.Client.Common.Plugin;
using TAS.Client.ViewModels;
using TAS.Server;

namespace TAS.Client
{
    public class MainWindowViewmodel : ViewModels.ViewmodelBase
    {
        readonly List<TabItem> _tabs;

        CompositionContainer _uiContainer;

        [ImportMany]
        IEnumerable<IUiPlugin> _plugins;

        public MainWindowViewmodel()
        {
            _tabs = new List<TabItem>();
            if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(new System.Windows.DependencyObject()))
            {
                _composePlugins();
                var engines = EngineController.Engines;
                if (engines != null)
                    foreach (Engine engine in engines)
                    {
                        TabItem newtab = new TabItem();
                        newtab.Header = engine.EngineName;
                        Debug.WriteLine(engine, "Creating viewmodel for");
                        var engineViewModel = new EngineViewmodel(engine, engine);
                        Debug.WriteLine(engine, "Creating commands for");
                        newtab.Content = engineViewModel.View;
                        _tabs.Add(newtab);

                        Debug.WriteLine(engine.MediaManager, "Creating tab for");
                        TabItem tabIngest = new TabItem();
                        tabIngest.Header = engine.EngineName + " - Media";
                        MediaManagerViewmodel newMediaManagerViewmodel = new MediaManagerViewmodel(engine.MediaManager, engine);
                        tabIngest.Content = newMediaManagerViewmodel.View;
                        _tabs.Add(tabIngest);

                        //Debug.WriteLine(engine.Templates, "Creating tab for");
                        //TabItem tabTemplates = new TabItem();
                        //tabTemplates.Header = engine.EngineName + " - Animacje";
                        //TemplatesView newTemplatesView = new TemplatesView();
                        //TemplatesViewmodel newTemplatesViewmodel = new TemplatesViewmodel(engine);
                        //newTemplatesView.DataContext = newTemplatesViewmodel;
                        //tabTemplates.Content = newTemplatesView;
                        //tcChannels.Items.Add(tabTemplates);
                    }
            }
        }

        private void _composePlugins()
        {
            try
            {
                var pluginPath = Path.GetFullPath(".\\Plugins");
                if (Directory.Exists(pluginPath))
                {
                    DirectoryCatalog catalog = new DirectoryCatalog(pluginPath);
                    _uiContainer = new CompositionContainer(catalog);
                    _uiContainer.SatisfyImportsOnce(this);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }

        }

        public IEnumerable<TabItem> Tabs { get { return _tabs; } }

        protected override void OnDispose() { }
    }
}
