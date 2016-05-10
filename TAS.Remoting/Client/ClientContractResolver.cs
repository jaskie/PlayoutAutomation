using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TAS.Remoting.Client
{
    public class ClientContractResolver: DefaultContractResolver
    {
        private readonly Type IDtoType = typeof(ProxyBase);
        private readonly JsonConverter IDtoConverter = new ClientSerializationConverter();
        protected override JsonContract CreateContract(Type objectType)
        {
            var contract =  base.CreateContract(objectType);
            if (IDtoType.IsAssignableFrom(objectType))
            {
                contract.Converter = IDtoConverter;
                contract.DefaultCreator = () => Activator.CreateInstance(objectType);
            }
            return contract;
        }
    }
}
