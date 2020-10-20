using System.ComponentModel.Composition;
using TAS.Common.Interfaces.Configurator;
using TAS.Database.Common;
using TAS.Database.Common.Interfaces;
using TAS.Server.CgElementsController.Configurator;

namespace TAS.Server.CgElementsController
{
    [Export(typeof(IPluginConfigurationProvider))]
    public class ConfigurationProvider : IPluginConfigurationProvider
    {
        public HibernationBinder Binder { get; } = new HibernationBinder(new System.Collections.Generic.Dictionary<System.Type, System.Type> {
            { typeof(CgElementsController), typeof(Configurator.Model.CgElementsController) },
            { typeof(CGElement), typeof(Configurator.Model.CgElement) }
        });

        public IPluginConfiguratorViewModel GetConfiguratorViewModel(IConfigEngine engine) => new CgElementsControllerViewModel(engine);        
    }
}
