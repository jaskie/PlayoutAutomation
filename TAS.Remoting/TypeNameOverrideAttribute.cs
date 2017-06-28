using System;

namespace TAS.Remoting
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class TypeNameOverrideAttribute: Attribute
    {
        public TypeNameOverrideAttribute(string typeName, string assemblyName = null)
        {
            TypeName = typeName;
            AssemblyName = assemblyName;
        }
        public string TypeName { get; }
        public string AssemblyName { get; }
    }
}
