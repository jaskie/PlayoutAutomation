using System;
using System.ComponentModel.Composition;
using TAS.Client.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Configurator;
using TAS.Database.Common;
using TAS.Database.Common.Interfaces;
using TAS.Server.VideoSwitch.Configurator;
using TAS.Server.VideoSwitch.Model;

namespace TAS.Server.VideoSwitch
{
    [Export(typeof(IPluginConfigurationProvider))]
    public class AtemConfigurationProvider : IPluginConfigurationProvider
    {
        static AtemConfigurationProvider()
        {
            WindowManager.Current.AddDataTemplate(typeof(AtemConfiguratorViewModel), typeof(AtemConfiguratorView));
        }
        
        public HibernationBinder Binder => VideoSwitchHibernationBinder.Current;

        public IPluginConfiguratorViewModel GetConfiguratorViewModel(IEngineProperties engine) => new AtemConfiguratorViewModel(engine);

        public Type GetPluginInterfaceType()
        {
            return typeof(IVideoSwitch);
        }
    }
}
