using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Threading;
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
                    Thread.CurrentPrincipal = new GenericPrincipal(new LocalUser(), new string[0]);
                    EngineController.Initialize();
                    var engines = EngineController.Engines;
                    Tabs = new List<ViewmodelBase>(engines.Select(engine => new ChannelViewmodel(engine, true, true, true)));
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
        
        public List<ViewmodelBase> Tabs { get; }

        protected override void OnDispose()
        {
            Tabs.ToList().ForEach(c => c.Dispose());
        }
    }
}
