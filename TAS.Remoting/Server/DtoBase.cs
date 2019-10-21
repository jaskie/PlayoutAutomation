//#undef DEBUG
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Xml.Serialization;

namespace TAS.Remoting.Server
{
    [JsonObject(ItemTypeNameHandling = TypeNameHandling.Objects, IsReference = true, MemberSerialization = MemberSerialization.OptIn)]
    public abstract class DtoBase: IDto
    {
        private static readonly ConcurrentDictionary<Guid, WeakReference<DtoBase>> AllDtos = new ConcurrentDictionary<Guid, WeakReference<DtoBase>>();

        internal static DtoBase FindDto(Guid guid)
        {
            if (AllDtos.TryGetValue(guid, out var reference) && reference.TryGetTarget(out var result))
                return result;
            return null;
        }

        protected DtoBase()
        {
            DtoGuid = Guid.NewGuid();
            AllDtos.TryAdd(DtoGuid, new WeakReference<DtoBase>(this));
        }

        [XmlIgnore]
        public Guid DtoGuid { get; }

        private int _disposed;

        ~DtoBase()
        {
            AllDtos.TryRemove(DtoGuid, out var _);
            Debug.WriteLine(this, $"{GetType().FullName} Finalized");
        }

        protected virtual bool SetField<T>(ref T field, T value, [CallerMemberName]string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            NotifyPropertyChanged(propertyName);
            return true;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public event EventHandler Disposed;

        public virtual void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != default(int))
                return;
            DoDispose();
            Disposed?.Invoke(this, EventArgs.Empty);
        }

        protected bool IsDisposed => _disposed != default(int);

        protected virtual void DoDispose()
        {
            
        }

        protected virtual void NotifyPropertyChanged([CallerMemberName]string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


    }


}
