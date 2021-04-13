using System.ComponentModel.Composition;
using TAS.Common.Interfaces.Configurator;
using TAS.Database.Common;
using TAS.Database.Common.Interfaces;
using TAS.Server.VideoSwitch.Configurator;
using TAS.Server.VideoSwitch.Model;

namespace TAS.Server.VideoSwitch
{
    [Export(typeof(IPluginConfigurationProvider))]
    public class ConfigurationProvider : IPluginConfigurationProvider
    {
        public HibernationBinder Binder { get; } = new HibernationBinder(new System.Collections.Generic.Dictionary<System.Type, System.Type> {
            { typeof(VideoSwitcher), typeof(VideoSwitcher) },
            { typeof(Router), typeof(Router) },
            { typeof(RouterPort), typeof(RouterPort) }
        });
        public IPluginConfiguratorViewModel GetConfiguratorViewModel(IConfigEngine engine) => new RouterConfiguratorViewModel();        
    }
}
