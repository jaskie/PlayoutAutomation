using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using TAS.Client.Common;
using TAS.Client.ViewModels;
using TAS.Server;
using resources = TAS.Client.Common.Properties.Resources;

namespace TAS.Client
{
    public class MainWindowViewmodel : ViewmodelBase
    {

        public MainWindowViewmodel()
        {
            if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                Tabs = new List<ViewmodelBase>(
                    EngineController.Engines.Select(engine => new ChannelViewmodel(engine, true, true, true)));
            }
        }

        public List<ViewmodelBase> Tabs { get; }

        protected override void OnDispose()
        {
            Tabs.ToList().ForEach(c => c.Dispose());
        }
    }
}
