using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        public string TypeName { get; private set; }
        public string AssemblyName { get; private set; }
    }
}
