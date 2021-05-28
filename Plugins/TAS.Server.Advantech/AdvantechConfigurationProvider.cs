using System;
using System.ComponentModel.Composition;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Configurator;
using TAS.Database.Common;
using TAS.Database.Common.Interfaces;
using TAS.Server.Advantech.Configurator;

namespace TAS.Server.Advantech
{
    [Export(typeof(IPluginConfigurationProvider))]
    public class AdvantechConfigurationProvider : IPluginConfigurationProvider
    {
        public HibernationBinder Binder { get; } = new HibernationBinder(new System.Collections.Generic.Dictionary<System.Type, System.Type> {
            { typeof(Gpi), typeof(Configurator.Model.Gpi) },
            { typeof(Model.GpiBinding), typeof(Configurator.Model.GpiBinding) }
        });

        public IPluginConfiguratorViewModel GetConfiguratorViewModel(IEngineProperties engine) => new GpiViewModel();

        public Type GetPluginInterfaceType()
        {
            throw new NotImplementedException();
        }

        IPluginConfiguratorViewModel IPluginConfigurationProvider.GetConfiguratorViewModel(IEngineProperties engine)
        {
            throw new NotImplementedException();
        }
    }
}
