using System;
using TAS.Database.Common.Interfaces;

namespace TAS.Server
{
    public class PluginTypeBinder : IPluginTypeBinder
    {
        public Type BindToType(string assemblyName, string typeName)
        {
            throw new NotImplementedException();
        }

        public Type GetBindedType(Type configType)
        {
            throw new NotImplementedException();
        }
    }
}
