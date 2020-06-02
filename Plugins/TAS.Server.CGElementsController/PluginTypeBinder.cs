using System;
using System.ComponentModel.Composition;
using TAS.Database.Common.Interfaces;

namespace TAS.Server
{
    [Export(typeof(IPluginTypeBinder))]
    public class PluginTypeBinder : IPluginTypeBinder
    {
        public bool BindToName(Type type, out string assemblyName, out string typeName)
        {
            var controllerType = typeof(TAS.Server.CgElementsController.CgElementsController);
            var configurationModelType = typeof(TAS.Client.Config.Model.CgElementsController); // can be type of model
            if (configurationModelType == type)
            {
                assemblyName = configurationModelType.AssemblyQualifiedName;
                typeName = configurationModelType.FullName;
                return true;
            }
            assemblyName = controllerType.AssemblyQualifiedName;
            typeName = controllerType.FullName;
            return false;
        }
    }
}
