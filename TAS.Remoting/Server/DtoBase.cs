#undef DEBUG
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Serialization;

namespace TAS.Remoting.Server
{
    [JsonObject(ItemTypeNameHandling = TypeNameHandling.Objects, IsReference = true, MemberSerialization = MemberSerialization.OptIn)]
    public abstract class DtoBase: IDto, INotifyPropertyChanged
    {
        [XmlIgnore]
        public Guid DtoGuid { get; private set; } = Guid.NewGuid();

#if DEBUG
        ~DtoBase()
        {
            Debug.WriteLine(this, string.Format("{0} Finalized", GetType().FullName));
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
        
        protected virtual void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event EventHandler Disposed;

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                DoDispose();
            }
        }

        private bool _disposed = false;

        protected bool IsDisposed { get { return _disposed; } }

        protected virtual void DoDispose()
        {
            Disposed?.Invoke(this, EventArgs.Empty);
        }

    }


}
