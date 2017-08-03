#undef DEBUG
using Newtonsoft.Json;
using System;
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
        [XmlIgnore]
        public Guid DtoGuid { get; } = Guid.NewGuid();

        private int _disposed;

#if DEBUG
        ~DtoBase()
        {
            Debug.WriteLine(this, $"{GetType().FullName} Finalized");
        }
#endif // DEBUG

        protected virtual bool SetField<T>(ref T field, T value, [CallerMemberName]string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            NotifyPropertyChanged(propertyName);
            return true;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        
        public event EventHandler Disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == default(int))
                DoDispose();
        }

        protected bool IsDisposed => _disposed != default(int);

        protected virtual void DoDispose()
        {
            Disposed?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


    }


}
