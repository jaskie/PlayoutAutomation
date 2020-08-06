using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using TAS.Database.Common.Interfaces;

namespace TAS.Server.VideoSwitch
{
    [Export(typeof(IPluginTypeBinder))]
    public class PluginTypeBinder : IPluginTypeBinder
    {
        private readonly List<Tuple<Type, Type>> _types = new List<Tuple<Type, Type>>()
        {
            new Tuple<Type, Type>(typeof(VideoSwitch), typeof(VideoSwitch))            
        };

        public Type GetBindedType(Type type)
        {
            foreach (var typePair in _types)
                if (typePair.Item1 == type)
                    return typePair.Item2;

            //uncomment in case of diffrent classes pair in _types
            //foreach (var typePair in _types)
            //    if (typePair.Item2 == type)
            //        return typePair.Item2;

            return null;
        }

        public Type BindToType(string assemblyName, string typeName)
        {
            return Type.GetType(typeName);
        }              
    }
}
