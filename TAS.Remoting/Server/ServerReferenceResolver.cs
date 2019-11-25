using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using Newtonsoft.Json.Serialization;

namespace TAS.Remoting.Server
{
    public class ServerReferenceResolver : IReferenceResolver, IDisposable
    {
        private readonly ConcurrentDictionary<Guid, DtoBase> _knownDtos = new ConcurrentDictionary<Guid, DtoBase>();
        private int _disposed;
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

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
            if (!(value is DtoBase dto))
                return;
            var id = new Guid(reference);
            _knownDtos[id] = dto;
            Logger.Trace("AddReference {0} for {1}", reference, value);
        }

        public string GetReference(object context, object value)
        {
            if (!(value is DtoBase dto)) return 
                    string.Empty;
            if (IsReferenced(context, value))
                return dto.DtoGuid.ToString();
            _knownDtos[dto.DtoGuid] = dto;
            dto.PropertyChanged += _referencePropertyChanged;
            dto.Disposed += _reference_Disposed;
            Logger.Trace("GetReference added {0} for {1}", dto.DtoGuid, value);
            return dto.DtoGuid.ToString();
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
                throw new UnresolvedReferenceException(id);
            Logger.Trace("ResolveReference {0} with {1}", reference, value);
            return value;
        }

        #endregion //IReferenceResolver

        public IDto ResolveReference(Guid reference)
        {
            if (!_knownDtos.TryGetValue(reference, out var p))
                throw new UnresolvedReferenceException(reference);
            return p;
        }

        public event EventHandler<PropertyChangedEventArgs> ReferencePropertyChanged;

        public event EventHandler ReferenceDisposed;

        private void _referencePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            ReferencePropertyChanged?.Invoke(sender, e);
        }

        private void _reference_Disposed(object sender, EventArgs e)
        {
            ReferenceDisposed?.Invoke(sender, EventArgs.Empty);
            if (!(sender is IDto dto) || !_knownDtos.TryRemove(dto.DtoGuid, out var disposed) || sender != disposed)
                return;
            disposed.PropertyChanged -= _referencePropertyChanged;
            disposed.Disposed -= _reference_Disposed;
            Logger.Trace("Reference resolver - object {0} disposed, generation is {1}", disposed, GC.GetGeneration(dto));
        }

    }

}
