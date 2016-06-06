using System;
using System.Collections.Generic;
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


        public MainWindowViewmodel()
        {
            _tabs = new List<TabItem>();
            if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(new System.Windows.DependencyObject()))
            {
                var engines = EngineController.Engines;
                if (engines != null)
                    foreach (Engine engine in engines)
                    {
                        TabItem newtab = new TabItem();
                        newtab.Header = engine.EngineName;
                        Debug.WriteLine(engine, "Creating viewmodel for");
                        var engineViewModel = new EngineViewmodel(engine, engine);
                        newtab.Content = engineViewModel.View;
                        _tabs.Add(newtab);

                        Debug.WriteLine(engine.MediaManager, "Creating tab for");
                        TabItem tabIngest = new TabItem();
                        tabIngest.Header = engine.EngineName + " - Media";
                        MediaManagerViewmodel newMediaManagerViewmodel = new MediaManagerViewmodel(engine.MediaManager, engine);
                        tabIngest.Content = newMediaManagerViewmodel.View;
                        _tabs.Add(tabIngest);
                    }
            }
        }


        public IEnumerable<TabItem> Tabs { get { return _tabs; } }

        protected override void OnDispose() { }
    }
}
