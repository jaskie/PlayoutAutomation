using System;
using System.ComponentModel.Composition;
using TAS.Database.Common.Interfaces;

namespace TAS.Server.CgElementsController
{
    [Export(typeof(IPluginTypeBinder))]
    public class PluginTypeBinder : IPluginTypeBinder
    {
        public bool BindToName(Type type, out string assemblyName, out string typeName)
        {
            var controllerType = typeof(TAS.Server.CgElementsController.CgElementsController);
            var configurationModelType = typeof(TAS.Server.CgElementsController.Configurator.Model.CgElementsController); // can be type of model
            if (configurationModelType == type)
            {
                assemblyName = configurationModelType.Assembly.FullName;
                typeName = configurationModelType.FullName;
                return true;
            }
            assemblyName = controllerType.Assembly.FullName;
            typeName = controllerType.FullName;
            return false;
        }

        public Type BindToType(string assemblyName, string typeName)
        {
            return Type.GetType(typeName);
        }
    }
}
