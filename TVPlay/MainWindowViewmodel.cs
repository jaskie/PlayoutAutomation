using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using TAS.Client.Common.Plugin;
using TAS.Client.ViewModels;
using TAS.Server;
using resources = TAS.Client.Common.Properties.Resources;

namespace TAS.Client
{
    public class MainWindowViewmodel : ViewModels.ViewmodelBase
    {
        readonly List<ChannelViewmodel> _channels;
        
        public MainWindowViewmodel()
        {
            if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(new System.Windows.DependencyObject()))
            {
                try
                {
                    var engines = EngineController.Engines;
                    _channels = new List<ChannelViewmodel>(engines.Select(engine => new ChannelViewmodel(engine)));
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
        
        public List<ChannelViewmodel> Channels { get { return _channels; } }

        protected override void OnDispose() { }
    }
}
