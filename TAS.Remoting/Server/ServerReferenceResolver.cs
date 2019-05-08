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
                if (!_knownDtos.TryRemove(key, out var value))
                    continue;
                RemoveDelegates(value);
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
            AttachDelegates(dto);
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

        public IDto ResolveReference(Guid reference)
        {
            if (!_knownDtos.TryGetValue(reference, out var p))
                throw new UnresolvedReferenceException(reference);
            return p;
        }

        public event EventHandler<WrappedEventArgs> ReferencePropertyChanged;

        public event EventHandler<WrappedEventArgs> ReferenceDisposed;

        private void _referencePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!(sender is DtoBase dto))
                throw new InvalidOperationException("Object provided is not DtoBase");
            ReferencePropertyChanged?.Invoke(this, new WrappedEventArgs(dto, e));
        }

        private void _reference_Disposed(object sender, EventArgs e)
        {

            if (!(sender is DtoBase dto))
                throw new InvalidOperationException("Object provided is not DtoBase");
            if (!_knownDtos.TryRemove(dto.DtoGuid, out var disposed))
                throw new InvalidOperationException("DtoBase wasn't in dictionary");
            if (!Equals(sender, disposed))
                throw new InvalidOperationException("Object in dictionary was different than was notifying");

            ReferenceDisposed?.Invoke(this, new WrappedEventArgs(dto, e));
            RemoveDelegates(disposed);

            Logger.Trace("Reference resolver - object {0} disposed, generation is {1}", disposed, GC.GetGeneration(dto));
        }

        private void AttachDelegates(IDto dto)
        {
            dto.PropertyChanged += _referencePropertyChanged;
            dto.Disposed += _reference_Disposed;
        }

        private void RemoveDelegates(IDto dto)
        {
            dto.PropertyChanged -= _referencePropertyChanged;
            dto.Disposed -= _reference_Disposed;
        }

    }

}
