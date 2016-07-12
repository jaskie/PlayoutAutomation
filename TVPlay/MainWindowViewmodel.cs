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
        readonly List<ChannelViewmodel> _channels;


        public MainWindowViewmodel()
        {
            if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(new System.Windows.DependencyObject()))
            {
                var engines = EngineController.Engines;
                _channels = new List<ChannelViewmodel>(engines.Select(engine => new ChannelViewmodel(engine)));
            }
        }
        
        public IEnumerable<ChannelViewmodel> Channels { get { return _channels; } }

        protected override void OnDispose() { }
    }
}
