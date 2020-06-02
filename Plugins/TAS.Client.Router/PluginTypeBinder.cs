using System;
using System.ComponentModel.Composition;
using TAS.Database.Common.Interfaces;

namespace TAS.Server.Router
{
    [Export(typeof(IPluginTypeBinder))]
    public class PluginTypeBinder : IPluginTypeBinder
    {
        public bool BindToName(Type type, out string assemblyName, out string typeName)
        {
            var routerType = typeof(Router);
            var configurationModelType = typeof(Router); // can be type of model
            if (configurationModelType == type)
            {
                assemblyName = routerType.AssemblyQualifiedName;
                typeName = routerType.FullName;
                return true;
            }
            assemblyName = null;
            typeName = null;
            return false;
        }
    }
}
