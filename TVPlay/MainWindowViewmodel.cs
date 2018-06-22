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
    public class MainWindowViewmodel : ViewModelBase
    {

        public MainWindowViewmodel()
        {
            if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                Tabs = new List<ViewModelBase>(
                    EngineController.Engines.Select(engine => 
                    {
                        SplashScreenView.Current?.Notify($"Creating {engine.EngineName}...");
                        return new ChannelViewmodel(engine, true, true);
                    }));
            }
        }

        public List<ViewModelBase> Tabs { get; } = new List<ViewModelBase>();

        protected override void OnDispose()
        {
            Tabs.ToList().ForEach(c => c.Dispose());
        }
    }
}
