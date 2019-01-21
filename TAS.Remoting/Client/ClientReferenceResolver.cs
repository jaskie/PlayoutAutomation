//#undef DEBUG

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Newtonsoft.Json.Serialization;

namespace TAS.Remoting.Client
{
    public class ClientReferenceResolver : IReferenceResolver, IDisposable
    {
        private readonly Dictionary<Guid, ProxyBase> _knownDtos = new Dictionary<Guid, ProxyBase>();
        private int _disposed;
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

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
            proxy.Disposed += _reference_Disposed;
            Debug.WriteLine("Added reference {0} for {1}", reference, value);
        }

        public string GetReference(object context, object value)
        {
            if (!(value is ProxyBase dto)) return
                string.Empty;
            lock (((IDictionary)_knownDtos).SyncRoot)
            {
                if (IsReferenced(context, value))
                    return dto.DtoGuid.ToString();
                _knownDtos[dto.DtoGuid] = dto;
            }
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

        private void _reference_Disposed(object sender, EventArgs e)
        {
            lock (((IDictionary) _knownDtos).SyncRoot)
                if (sender is IDto dto && _knownDtos.TryGetValue(dto.DtoGuid, out var _) && sender == dto)
                {
                    _knownDtos.Remove(dto.DtoGuid);
                    dto.Disposed -= _reference_Disposed;
                    Logger.Trace("Reference resolver - object {0} disposed, generation is {1}", dto, GC.GetGeneration(dto));
                }
        }

    }

}
