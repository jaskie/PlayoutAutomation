using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using TAS.Database.Common.Interfaces;

namespace TAS.Database.Common
{
    public class HibernationSerializationBinder : ISerializationBinder
    {
        private readonly IEnumerable<IPluginTypeBinder> _pluginTypeResolvers;

        public HibernationSerializationBinder(IEnumerable<IPluginTypeBinder> pluginTypeResolvers)
        {
            _pluginTypeResolvers = pluginTypeResolvers;
        }
        public void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            foreach (var resolver in _pluginTypeResolvers)
                if (resolver.BindToName(serializedType, out assemblyName, out typeName))
                    return;
            assemblyName = serializedType.Assembly.FullName;
            typeName = serializedType.FullName;
        }

        public Type BindToType(string assemblyName, string typeName)
        {
            foreach (var resolver in _pluginTypeResolvers)
            {
                var resolvedType = resolver.BindToType(assemblyName, typeName);
                if (resolvedType != null)
                    return resolvedType;
            }
                
            return Type.GetType($"{typeName},{assemblyName}", true);
        }
    }
}
