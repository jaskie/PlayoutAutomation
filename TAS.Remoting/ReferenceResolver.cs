//#undef DEBUG
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;

namespace TAS.Remoting
{
    public class ReferenceResolver : IReferenceResolver, IDisposable
    {
        private readonly ConcurrentDictionary<Guid, IDto> _knownDtos = new ConcurrentDictionary<Guid, IDto>();
        private int _disposed;

        public ReferenceResolver()
        {
            Debug.WriteLine("Created ReferenceResolver");
        }

#if DEBUG
        ~ReferenceResolver() {
            Debug.WriteLine("Finalized ReferenceResolver");
        }
#endif
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != default(int))
                return;
            var allKeys = _knownDtos.Keys;
            foreach (var key in allKeys)
            {
                if (!_knownDtos.TryRemove(key, out var value))
                    continue;
                value.PropertyChanged -= _referencePropertyChanged;
                value.Disposed -= _reference_Disposed;
            }
        }


        #region IReferenceResolver
        public void AddReference(object context, string reference, object value)
        {
            if (!(value is IDto p))
                return;
            Guid id = new Guid(reference);
            _knownDtos[id] = (IDto)value;
            if ((p as Client.ProxyBase)?.DtoGuid == Guid.Empty)
                ((Client.ProxyBase)p).DtoGuid = new Guid(reference);
            Debug.WriteLine("Added reference {0} for {1}", reference, value);
        }

        public string GetReference(object context, object value)
        {
            if (!(value is IDto p)) return 
                    string.Empty;
            if (IsReferenced(context, value))
                return p.DtoGuid.ToString();
            _knownDtos[p.DtoGuid] = p;
            p.PropertyChanged += _referencePropertyChanged;
            p.Disposed += _reference_Disposed;
            return p.DtoGuid.ToString();
        }


        public bool IsReferenced(object context, object value)
        {
            if (value is IDto p && !p.DtoGuid.Equals(Guid.Empty))
                return _knownDtos.ContainsKey(p.DtoGuid);
            return false;
        }

        public object ResolveReference(object context, string reference)
        {
            var id = new Guid(reference);
            if (!_knownDtos.TryGetValue(id, out var value))
                Debug.WriteLine("Unresolved reference {0}", reference);
            Debug.WriteLine("Resolved reference {0} with {1}", reference, value);
            return value;
        }

        #endregion //IReferenceResolver

        #region Server-side methods
        public IDto ResolveReference(Guid reference)
        {
            if (!_knownDtos.TryGetValue(reference, out var p))
                Debug.WriteLine("Unresolved reference {0}", reference);
            return p;
        }

        private void _referencePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            ReferencePropertyChanged?.Invoke(sender, e);
        }

        private void _reference_Disposed(object sender, EventArgs e)
        {
            ReferenceDisposed?.Invoke(sender, EventArgs.Empty);
            if (sender is IDto dto && _knownDtos.TryRemove(dto.DtoGuid, out var disposed) && sender == disposed)
            {
                disposed.PropertyChanged -= _referencePropertyChanged;
                disposed.Disposed -= _reference_Disposed;
                Debug.WriteLine(disposed, $"Reference resolver - object {disposed.DtoGuid} disposed, generation is {GC.GetGeneration(dto)}");
            }
        }
        #endregion // Server-side methods

        #region Client-side methods
        internal IDto RemoveReference(Guid reference)
        {
            _knownDtos.TryRemove(reference, out var removed);
            return removed;
        }
        #endregion //Client-side methods

        public event EventHandler<PropertyChangedEventArgs> ReferencePropertyChanged;
        public event EventHandler ReferenceDisposed;

    }

}
