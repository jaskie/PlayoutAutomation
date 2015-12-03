using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using TAS.Server.Interfaces;
using TAS.Client.Model;
using System.Diagnostics;

namespace TAS.Client.Converters
{
    abstract class DtoCreationConverter<T> : CustomCreationConverter<T> where T : IDto
    {
        protected readonly IRemoteClient Client;
        public DtoCreationConverter(IRemoteClient client)
        {
            Client = client;
        }

        ConcurrentDictionary<Guid, IDto> _knownObjects = new ConcurrentDictionary<Guid, IDto>();
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            object deserialized = base.ReadJson(reader, objectType, existingValue, serializer);
            if (deserialized != null)
            {
                IDto oldObject;
                if (_knownObjects.TryGetValue(((IDto)deserialized).DtoGuid, out oldObject))
                {
                    Debug.WriteLine(oldObject, "Reused");
                    Debug.Write(new StackTrace());
                    return oldObject;
                }
                else
                {
                    _knownObjects[((IDto)deserialized).DtoGuid] = (IDto)deserialized;
                    ProxyBase proxy = deserialized as ProxyBase;
                    if (proxy != null)
                        proxy.SetClient(Client, _knownObjects);
                }
            }
            Debug.WriteLine(deserialized, "Client: created new");
            return deserialized;
        }
    }
}
