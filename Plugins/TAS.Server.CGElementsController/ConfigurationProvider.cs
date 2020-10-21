using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using TAS.Client.Common;
using TAS.Common.Interfaces.Configurator;
using TAS.Database.Common;
using TAS.Database.Common.Interfaces;
using TAS.Server.CgElementsController.Configurator;

namespace TAS.Server.CgElementsController
{
    [Export(typeof(IPluginConfigurationProvider))]
    public class ConfigurationProvider : IPluginConfigurationProvider
    {
        static ConfigurationProvider()
        {
            WindowManager.Current.AddDataTemplate(typeof(CgElementsControllerViewModel), typeof(CgElementsControllerView));
        }

        public HibernationBinder Binder { get; } = new HibernationBinder(new Dictionary<Type, Type> {
            { typeof(CgElementsController), typeof(Configurator.Model.CgElementsController) },
            { typeof(CGElement), typeof(Configurator.Model.CgElement) }
        });

        public IPluginConfiguratorViewModel GetConfiguratorViewModel(IConfigEngine engine) => new CgElementsControllerViewModel(engine);

        public Type GetPluginModelType()
        {
            return typeof(CgElementsController);
        }
    }
}
