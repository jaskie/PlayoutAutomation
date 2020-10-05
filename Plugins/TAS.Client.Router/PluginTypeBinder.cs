using System;
using System.Collections.Generic;
using TAS.Common.Interfaces;
using TAS.Server.VideoSwitch.Model;

namespace TAS.Server.VideoSwitch
{    
    internal class PluginTypeBinder : IPluginTypeBinder
    {
        private readonly List<Tuple<Type, Type>> _types = new List<Tuple<Type, Type>>()
        {
            new Tuple<Type, Type>(typeof(VideoSwitcher), typeof(VideoSwitcher)),
            new Tuple<Type, Type>(typeof(Router), typeof(Router)),
            new Tuple<Type, Type>(typeof(RouterPort), typeof(RouterPort))
        };

        public Type GetBindedType(Type type)
        {
            foreach (var typePair in _types)
                if (typePair.Item1 == type)
                    return typePair.Item2;
            
            foreach (var typePair in _types)
                if (typePair.Item2 == type)
                    return typePair.Item2;

            return null;
        }

        public Type BindToType(string assemblyName, string typeName)
        {
            return Type.GetType(typeName);
        }              
    }
}
