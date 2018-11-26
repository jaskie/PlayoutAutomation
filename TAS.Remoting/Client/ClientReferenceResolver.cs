//#undef DEBUG

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using Newtonsoft.Json.Serialization;

namespace TAS.Remoting.Client
{
    public class ClientReferenceResolver : IReferenceResolver, IDisposable
    {
        private readonly Dictionary<Guid, ProxyBase> _knownDtos = new Dictionary<Guid, ProxyBase>();
        private int _disposed;

        public ClientReferenceResolver()
        {
            Debug.WriteLine("Created ReferenceResolver");
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != default(int))
                return;
            lock (((IDictionary) _knownDtos).SyncRoot)
            {
                foreach (var dto in _knownDtos)
                {
                    dto.Value.PropertyChanged -= _referencePropertyChanged;
                    dto.Value.Disposed -= _reference_Disposed;
                }
                _knownDtos.Clear();
            }
        }


        #region IReferenceResolver
        public void AddReference(object context, string reference, object value)
        {
            if (!(value is ProxyBase proxy))
                return;
            var id = new Guid(reference);
            proxy.DtoGuid = id;
            lock (((IDictionary)_knownDtos).SyncRoot)
                _knownDtos[id] = proxy;
            Debug.WriteLine("Added reference {0} for {1}", reference, value);
        }

        public string GetReference(object context, object value)
        {
            if (!(value is ProxyBase dto)) return 
                    string.Empty;
            lock (((IDictionary) _knownDtos).SyncRoot)
            {
                if (IsReferenced(context, value))
                    return dto.DtoGuid.ToString();
                _knownDtos[dto.DtoGuid] = dto;
            }
            dto.PropertyChanged += _referencePropertyChanged;
            dto.Disposed += _reference_Disposed;
            return dto.DtoGuid.ToString();
        }


        public bool IsReferenced(object context, object value)
        {
            lock (((IDictionary) _knownDtos).SyncRoot)
                if (value is IDto p && !p.DtoGuid.Equals(Guid.Empty))
                    return _knownDtos.ContainsKey(p.DtoGuid);
            return false;
        }

        public object ResolveReference(object context, string reference)
        {
            var id = new Guid(reference);
            lock (((IDictionary) _knownDtos).SyncRoot)
            {
                if (!_knownDtos.TryGetValue(id, out var value))
                    throw new UnresolvedReferenceException("ResolveReference failed", id);
                Debug.WriteLine("Resolved reference {0} with {1}", reference, value);
                return value;
            }
        }

        #endregion //IReferenceResolver

        internal ProxyBase ResolveReference(Guid reference)
        {
            lock (((IDictionary) _knownDtos).SyncRoot)
            {
                if (!_knownDtos.TryGetValue(reference, out var p))
                    throw new UnresolvedReferenceException("ResolveReference failed", reference);
                return p;
            }
        }

        private void _referencePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            ReferencePropertyChanged?.Invoke(sender, e);
        }

        private void _reference_Disposed(object sender, EventArgs e)
        {
            ReferenceDisposed?.Invoke(sender, EventArgs.Empty);
            lock (((IDictionary) _knownDtos).SyncRoot)
                if (sender is IDto dto && _knownDtos.TryGetValue(dto.DtoGuid, out var disposed) && sender == disposed)
                {
                    _knownDtos.Remove(dto.DtoGuid);
                    disposed.PropertyChanged -= _referencePropertyChanged;
                    disposed.Disposed -= _reference_Disposed;
                    Debug.WriteLine(disposed, $"Reference resolver - object {disposed.DtoGuid} disposed, generation is {GC.GetGeneration(dto)}");
                }
        }

        #region Client-side methods
        internal IDto RemoveReference(Guid reference)
        {
            lock (((IDictionary) _knownDtos).SyncRoot)
            {
                if (!_knownDtos.TryGetValue(reference, out var removed))
                    throw new UnresolvedReferenceException("ResolveReference failed", reference);
                _knownDtos.Remove(reference);
                return removed;
            }
        }
        #endregion //Client-side methods

        public event EventHandler<PropertyChangedEventArgs> ReferencePropertyChanged;
        public event EventHandler ReferenceDisposed;

    }

}
