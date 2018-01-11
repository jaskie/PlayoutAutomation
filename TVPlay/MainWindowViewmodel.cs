using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using TAS.Client.Common;
using TAS.Client.ViewModels;
using TAS.Client.Views;
using TAS.Server;

namespace TAS.Client
{
    public class MainWindowViewmodel : ViewmodelBase
    {

        public MainWindowViewmodel()
        {
            if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                Tabs = new List<ViewmodelBase>(
                    EngineController.Engines.Select(engine => 
                    {
                        SplashScreenView.Current?.Notify($"Creating {engine.EngineName}...");
                        return new ChannelViewmodel(engine, true, true);
                    }));
            }
        }

        public List<ViewmodelBase> Tabs { get; } = new List<ViewmodelBase>();

        protected override void OnDispose()
        {
            Tabs.ToList().ForEach(c => c.Dispose());
        }
    }
}
