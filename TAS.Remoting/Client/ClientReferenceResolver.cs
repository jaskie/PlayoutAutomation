using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Newtonsoft.Json.Serialization;
using TAS.Common;

namespace TAS.Remoting.Client
{
    public class ClientReferenceResolver : IReferenceResolver, IDisposable
    {
        private readonly Dictionary<Guid, ProxyBase> _knownDtos = new Dictionary<Guid, ProxyBase>();
        private int _disposed;
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != default(int))
                return;
            lock (((IDictionary) _knownDtos).SyncRoot)
            {
                foreach (var dto in _knownDtos)
                {
                    dto.Value.Disposed -= Proxy_Disposed;
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
            proxy.Disposed += Proxy_Disposed;
            proxy.Finalized += Proxy_Finalized;
            Logger.Trace("AddReference {0} for {1}", reference, value);
        }

        public string GetReference(object context, object value)
        {
            if (!(value is ProxyBase proxy)) return
                string.Empty;
            lock (((IDictionary)_knownDtos).SyncRoot)
            {
                if (IsReferenced(context, value))
                    return proxy.DtoGuid.ToString();
                _knownDtos[proxy.DtoGuid] = proxy;
            }
            proxy.Disposed += Proxy_Disposed;
            proxy.Finalized += Proxy_Finalized;
            Logger.Warn("GetReference added {0} for {1}", proxy.DtoGuid, value);
            return proxy.DtoGuid.ToString();
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
                    throw new UnresolvedReferenceException(id);
                Logger.Trace("Resolved reference {0} with {1}", reference, value);
                return value;
            }
        }

        #endregion //IReferenceResolver

        internal event EventHandler<EventArgs<ProxyBase>> ReferenceFinalized;

        internal ProxyBase ResolveReference(Guid reference)
        {
            lock (((IDictionary) _knownDtos).SyncRoot)
            {
                if (_knownDtos.TryGetValue(reference, out var p))
                    return p;
                return null;
            }
        }

        private void Proxy_Disposed(object sender, EventArgs e)
        {
            lock (((IDictionary) _knownDtos).SyncRoot)
                if (sender is ProxyBase proxy && _knownDtos.TryGetValue(proxy.DtoGuid, out var _) && sender == proxy)
                {
                    _knownDtos.Remove(proxy.DtoGuid);
                    proxy.Disposed -= Proxy_Disposed;
                    Logger.Trace("Reference resolver - object {0} disposed, generation is {1}", proxy, GC.GetGeneration(proxy));
                }
        }

        private void Proxy_Finalized(object sender, EventArgs e)
        {
            Debug.Assert(sender is ProxyBase);
            Proxy_Disposed(sender, e);
            ((ProxyBase) sender).Finalized -= Proxy_Finalized;
            ReferenceFinalized?.Invoke(this, new EventArgs<ProxyBase>((ProxyBase)sender));
        }

    }

}
