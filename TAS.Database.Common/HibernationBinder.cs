using System;
using System.Collections.Generic;
using System.Linq;

namespace TAS.Database.Common
{

    /// <summary>
    /// Binder used in configuration mode to map configuration time models to runtime types
    /// </summary>
    public class HibernationBinder
    {
        private readonly Dictionary<Type, Type> _typeMappings;
        
        /// <summary>
        /// Creates binder
        /// </summary>
        /// <param name="typeMappings">List of types (provided as keys) and its configuration models (provided as values)</param>
        public HibernationBinder(Dictionary<Type, Type> typeMappings)
        {
            _typeMappings = typeMappings;
        }

        public Type GetRuntimeType(Type type) => 
            _typeMappings.FirstOrDefault(m => m.Value == type).Key;

        public Type GetModelType(string assemblyName, string typeName) => 
            _typeMappings.FirstOrDefault(m => m.Key.Assembly.GetName().Name == assemblyName && m.Key.FullName == typeName).Value;
    }
}
