using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;

namespace TAS.Database.Common
{
    public class PluginSerializationBinder : ISerializationBinder
    {
        private readonly IEnumerable<HibernationBinder> _pluginHibernationBinders;

        public PluginSerializationBinder(IEnumerable<HibernationBinder> pluginHibernationBinders)
        {
            _pluginHibernationBinders = pluginHibernationBinders;
        }
        
        public void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            foreach (var resolver in _pluginHibernationBinders)
            {
                var resolvedType = resolver.GetRuntimeType(serializedType);
                if (resolvedType != null)
                {
                    assemblyName = resolvedType.Assembly.FullName;
                    typeName = resolvedType.FullName;
                    return;
                }
            }
            assemblyName = serializedType.Assembly.FullName;
            typeName = serializedType.FullName;
        }

        public Type BindToType(string assemblyName, string typeName)
        {
            foreach (var resolver in _pluginHibernationBinders)
            {
                var resolvedType = resolver.GetModelType(assemblyName, typeName);
                if (resolvedType != null)
                    return resolvedType;
            }
            return Type.GetType($"{typeName},{assemblyName}");
        }
    }
}
