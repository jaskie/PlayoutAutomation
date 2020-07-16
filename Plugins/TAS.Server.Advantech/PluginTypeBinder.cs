using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using TAS.Database.Common.Interfaces;
using TAS.Server.Advantech.Model;

namespace TAS.Server.Advantech
{
    [Export(typeof(IPluginTypeBinder))]
    public class PluginTypeBinder : IPluginTypeBinder
    {
        private readonly List<Tuple<Type, Type>> _types = new List<Tuple<Type, Type>>()
        {
            new Tuple<Type, Type>(typeof(Configurator.Model.Gpi), typeof(Gpi)),
            new Tuple<Type, Type>(typeof(Configurator.Model.GpiBinding), typeof(GpiBinding))
        };
        public Type GetBindedType(Type type)
        {
            foreach (var typePair in _types)
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
