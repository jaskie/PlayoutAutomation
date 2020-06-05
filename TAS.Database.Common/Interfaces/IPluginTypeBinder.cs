using System;

namespace TAS.Database.Common.Interfaces
{
    public interface IPluginTypeBinder
    {
        bool BindToName(Type type, out string assemblyName, out string typeName);
        Type BindToType(string assemblyName, string typeName);
    }
}
