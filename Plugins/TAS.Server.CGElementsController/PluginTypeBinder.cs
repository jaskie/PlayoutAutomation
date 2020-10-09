using System;
using System.Collections.Generic;
using TAS.Common.Interfaces;

namespace TAS.Server.CgElementsController
{    
    internal class PluginTypeBinder : IPluginTypeBinder
    {
        private readonly List<Tuple<Type, Type>> _types = new List<Tuple<Type, Type>>()
        {
            new Tuple<Type, Type>(typeof(Configurator.Model.CgElementsController), typeof(CgElementsController)),
            new Tuple<Type, Type>(typeof(Configurator.Model.CgElement), typeof(Model.CGElement))
        };
        public Type GetBindedType(Type type)
        {          
            foreach(var typePair in _types)            
                if (typePair.Item1 == type)
                    return typePair.Item2;
            

            foreach (var typePair in _types)            
                if (typePair.Item2 == type)
                    return typePair.Item1;
            
            return null;
        }

        public Type BindToType(string assemblyName, string typeName)
        {
            return Type.GetType(typeName);
        }        
    }
}
