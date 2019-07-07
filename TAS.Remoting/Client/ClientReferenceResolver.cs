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
        private readonly Dictionary<Guid, WeakReference<ProxyBase>> _knownDtos = new Dictionary<Guid, WeakReference<ProxyBase>>();
        private int _disposed;
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != default(int))
                return;
            lock (((IDictionary) _knownDtos).SyncRoot)
            {
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
                _knownDtos[id] = new WeakReference<ProxyBase>(proxy);
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
                _knownDtos[proxy.DtoGuid] = new WeakReference<ProxyBase>(proxy);
            }
            proxy.Finalized += Proxy_Finalized;
            Logger.Warn("GetReference added {0} for {1}", proxy.DtoGuid, value);
            return proxy.DtoGuid.ToString();
        }


        public bool IsReferenced(object context, object value)
        {
            if (!(value is IDto dto))
                return false;
            lock (((IDictionary) _knownDtos).SyncRoot)
                if (_knownDtos.TryGetValue(dto.DtoGuid, out var reference))
                {
                    if (reference.TryGetTarget(out var _))
                        return true;
                    _knownDtos.Remove(dto.DtoGuid);
                }
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
                if (value.TryGetTarget(out var target))
                    return target;
                _knownDtos.Remove(id);
                return null;
            }
        }

        #endregion //IReferenceResolver

        internal event EventHandler<EventArgs<ProxyBase>> ReferenceFinalized;

        internal ProxyBase ResolveReference(Guid reference)
        {
            lock (((IDictionary) _knownDtos).SyncRoot)
            {
                if (!_knownDtos.TryGetValue(reference, out var p))
                    return null;
                if (p.TryGetTarget(out var target))
                    return target;
                _knownDtos.Remove(reference);
                return null;
            }
        }

        private void Proxy_Finalized(object sender, EventArgs e)
        {
            Debug.Assert(sender is ProxyBase);
            ((ProxyBase)sender).Finalized -= Proxy_Finalized;
            try
            {
                lock (((IDictionary) _knownDtos).SyncRoot)
                    if (_knownDtos.TryGetValue(((ProxyBase) sender).DtoGuid, out var _))
                    {
                        _knownDtos.Remove(((ProxyBase) sender).DtoGuid);
                        Logger.Trace("Reference resolver - object {0} disposed, generation is {1}", sender,
                            GC.GetGeneration(sender));
                    }
                ReferenceFinalized?.Invoke(this, new EventArgs<ProxyBase>((ProxyBase) sender));
            }
            catch
            {
                // ignored because invoked in garbage collector thread
            }
        }

    }

}
