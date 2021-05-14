using System;
using System.ComponentModel.Composition;
using TAS.Client.Common;
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
        static ConfigurationProvider()
        {
            WindowManager.Current.AddDataTemplate(typeof(RouterConfiguratorViewModel), typeof(RouterConfiguratorView));
        }

        public HibernationBinder Binder { get; } = new HibernationBinder(new System.Collections.Generic.Dictionary<Type, Type> {
            { typeof(VideoSwitcher), typeof(VideoSwitcher) },
            { typeof(Router), typeof(Router) },
            { typeof(RouterPort), typeof(RouterPort) }
        });
        public IPluginConfiguratorViewModel GetConfiguratorViewModel(IConfigEngine engine) => new RouterConfiguratorViewModel(engine);

        public Type GetPluginModelType()
        {
            return typeof(RouterBase);
        }
    }
}
