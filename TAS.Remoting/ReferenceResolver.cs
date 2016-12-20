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
            if (p != null && p.DtoGuid.Equals(Guid.Empty))
            {
                Guid id = new Guid(reference);
                p.DtoGuid = id;
                _knownDtos[id] = (IDto)value;
            }
        }

        private void _referencePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            ReferencePropertyChanged?.Invoke(sender, e);
        }

        public string GetReference(object context, object value)
        {
            IDto p = value as IDto;
            if (p != null)
            {
                if (p.DtoGuid == Guid.Empty)
                {
                    p.DtoGuid = Guid.NewGuid();
                    _knownDtos[p.DtoGuid] = p;
                    p.PropertyChanged += _referencePropertyChanged;
                    p.Disposed += _reference_Disposed;
                }
                return p.DtoGuid.ToString();
            }
            return string.Empty;
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
