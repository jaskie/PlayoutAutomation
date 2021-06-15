using System;
using System.ComponentModel.Composition;
using TAS.Client.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Configurator;
using TAS.Database.Common;
using TAS.Database.Common.Interfaces;
using TAS.Server.VideoSwitch.Configurator;

namespace TAS.Server.VideoSwitch
{
    [Export(typeof(IPluginConfigurationProvider))]
    public class SmartVideoHubConfigurationProvider : IPluginConfigurationProvider
    {
        static SmartVideoHubConfigurationProvider()
        {
            WindowManager.Current.AddDataTemplate(typeof(SmartVideoHubConfiguratorViewModel), typeof(ConfiguratorView));
        }

        public HibernationBinder Binder => VideoSwitchHibernationBinder.Current;

        public IPluginConfiguratorViewModel GetConfiguratorViewModel(IEngineProperties engine) => new SmartVideoHubConfiguratorViewModel(engine);

        public Type GetPluginInterfaceType()
        {
            return typeof(IVideoSwitch);
        }
    }
}
