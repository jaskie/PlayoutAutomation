using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TAS.Remoting
{
    public class ReferenceResolver : IReferenceResolver
    {
        readonly ConcurrentDictionary<Guid, IDto> _knownDtos = new ConcurrentDictionary<Guid, IDto>();
        public void AddReference(object context, string reference, object value)
        {
            if (value is IDto)
            {
                Guid id = new Guid(reference);
                _knownDtos[id] = (IDto)value;
            }
        }

        public string GetReference(object context, object value)
        {
            IDto p = value as IDto;
            if (p != null)
            {
                _knownDtos[p.DtoGuid] = p;
                return p.DtoGuid.ToString();
            }
            return string.Empty;
        }

        public bool IsReferenced(object context, object value)
        {
            IDto p = value as IDto;
            if (p != null)
                return _knownDtos.ContainsKey(p.DtoGuid);
            return false;
        }

        public object ResolveReference(object context, string reference)
        {
            Guid id = new Guid(reference);
            IDto p;
            _knownDtos.TryGetValue(id, out p);
            return p;
        }

        public IDto ResolveReference(Guid reference)
        {
            IDto p;
            _knownDtos.TryGetValue(reference, out p);
            return p;
        }
    }
}
