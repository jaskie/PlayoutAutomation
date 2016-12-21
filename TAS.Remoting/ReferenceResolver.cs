using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace TAS.Remoting
{
    public class ReferenceResolver : IReferenceResolver, IDisposable
    {
        public ReferenceResolver()
        {
            Debug.WriteLine("Created ReferenceResolver");
        }

#if DEBUG
        ~ReferenceResolver() {
            Debug.WriteLine("Finalized ReferenceResolver");
        }
#endif
        readonly ConcurrentDictionary<Guid, IDto> _knownDtos = new ConcurrentDictionary<Guid, IDto>();
        public void AddReference(object context, string reference, object value)
        {
            IDto p = value as IDto;
            if (p != null)
            {
                Guid id = new Guid(reference);
                _knownDtos[id] = (IDto)value;
                if ((p as Client.ProxyBase)?.DtoGuid == Guid.Empty)
                    ((Client.ProxyBase)p).DtoGuid = new Guid(reference);
            }
        }

        public string GetReference(object context, object value)
        {
            IDto p = value as IDto;
            if (p != null)
            {
                _knownDtos[p.DtoGuid] = p;
                p.PropertyChanged += _referencePropertyChanged;
                p.Disposed += _reference_Disposed;
                return p.DtoGuid.ToString();
            }
            return string.Empty;
        }


        public bool IsReferenced(object context, object value)
        {
            IDto p = value as IDto;
            if (p != null && !p.DtoGuid.Equals(Guid.Empty))
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

        private void _referencePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            ReferencePropertyChanged?.Invoke(sender, e);
        }

        private void _reference_Disposed(object sender, EventArgs e)
        {
            IDto disposed;
            if (sender is IDto && _knownDtos.TryRemove(((IDto)sender).DtoGuid, out disposed) && sender == disposed)
            {
                disposed.PropertyChanged -= _referencePropertyChanged;
                disposed.Disposed -= _reference_Disposed;
            }
        }

        public event EventHandler<PropertyChangedEventArgs> ReferencePropertyChanged;

        private bool _disposed = false;
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                var allKeys = _knownDtos.Keys;
                foreach (var key in allKeys)
                {
                    IDto value;
                    if (_knownDtos.TryRemove(key, out value))
                    {
                        value.PropertyChanged -= _referencePropertyChanged;
                        value.Disposed -= _reference_Disposed;
                    }
                }
            }
        }
    }

}
