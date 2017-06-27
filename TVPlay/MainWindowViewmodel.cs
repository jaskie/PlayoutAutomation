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
                try
                {
                    EngineController.Initialize();
                    var engines = EngineController.Engines;
                    Channels = new List<ChannelViewmodel>(engines.Select(engine => new ChannelViewmodel(engine, true, true, true)));
                }
                catch (TypeInitializationException e)
                {
                    MessageBox.Show(string.Format(resources._message_CantInitializeEngines, e.InnerException), resources._caption_Error, MessageBoxButton.OK, MessageBoxImage.Error);
                    Application.Current.Shutdown(1);
                }
                catch (Exception e)
                {
                    MessageBox.Show(string.Format(resources._message_CantInitializeEngines, e), resources._caption_Error, MessageBoxButton.OK, MessageBoxImage.Error);
                    Application.Current.Shutdown(1);
                }
            }
        }
        
        public List<ChannelViewmodel> Channels { get; }

        protected override void OnDispose()
        {
            Channels.ToList().ForEach(c => c.Dispose());
        }
    }
}
