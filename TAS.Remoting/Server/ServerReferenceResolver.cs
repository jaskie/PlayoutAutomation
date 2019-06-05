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


#if DEBUG
        ~ServerReferenceResolver()
        {
            Debug.WriteLine("Finalized: {0}", this);
        }
#endif
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != default(int))
                return;
            var allKeys = _knownDtos.Keys;
            foreach (var key in allKeys)
            {
                if (!_knownDtos.TryRemove(key, out var removed))
                    continue;
                removed.PropertyChanged -= Dto_PropertyChanged;
            }
        }

        #region IReferenceResolver
        public void AddReference(object context, string reference, object value)
        {
            throw new InvalidOperationException(nameof(AddReference));
        }

        public string GetReference(object context, object value)
        {
            if (!(value is DtoBase dto)) return 
                    string.Empty;
            if (IsReferenced(context, value))
                return dto.DtoGuid.ToString();
            dto.PropertyChanged += Dto_PropertyChanged;
            _knownDtos[dto.DtoGuid] = dto;
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

        public DtoBase ResolveReference(Guid reference)
        {
            if (!_knownDtos.TryGetValue(reference, out var p))
                throw new UnresolvedReferenceException(reference);
            return p;
        }

        public event EventHandler<WrappedEventArgs> ReferencePropertyChanged;

        private void Dto_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!(sender is DtoBase dto))
                throw new InvalidOperationException("Object provided is not DtoBase");
            ReferencePropertyChanged?.Invoke(this, new WrappedEventArgs(dto, e));
        }


        public void RemoveReference(IDto dto)
        {
            if (_knownDtos.TryRemove(dto.DtoGuid, out var removed))
                removed.PropertyChanged -= Dto_PropertyChanged;
        }
    }

}
