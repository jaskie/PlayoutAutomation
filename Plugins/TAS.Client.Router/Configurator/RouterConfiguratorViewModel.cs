using System.ComponentModel.Composition;
using TAS.Client.Common;
using TAS.Common.Interfaces;

namespace TAS.Server.Router.Configurator
{
    [Export(typeof(IPluginConfigurator))]
    public class RouterConfiguratorViewModel : ViewModelBase, IPluginConfigurator
    {
        public string PluginName => "Router";

        public bool IsEnabled { get; set; }        

        public void Initialize()
        {
            
        }

        public void Save()
        {
            
        }

        protected override void OnDispose()
        {            
        }
    }
}
