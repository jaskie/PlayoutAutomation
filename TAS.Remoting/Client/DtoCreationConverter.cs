using Newtonsoft.Json.Converters;
using System;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Diagnostics;
using TAS.Remoting;

namespace TAS.Remoting.Client
{
    public abstract class DtoCreationConverter<T> : CustomCreationConverter<T> where T : IDto
    {
        protected readonly RemoteClient Client;
        public DtoCreationConverter(RemoteClient client)
        {
            Client = client;
        }
        
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            object deserialized = base.ReadJson(reader, objectType, existingValue, serializer);
            if (deserialized != null)
            {
                IDto oldObject;
                if (Client.TryGetObject(((IDto)deserialized).DtoGuid, out oldObject))
                {
                    Debug.WriteLine(oldObject, "Reused");
                    return oldObject;
                }
                else
                    Client.SetObject(deserialized as ProxyBase);
            }
            Debug.WriteLine(deserialized, "Client: created new");
            return deserialized;
        }
    }
}
