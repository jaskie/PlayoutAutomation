#undef DEBUG
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace TAS.Remoting.Server
{
    [JsonObject(ItemTypeNameHandling = TypeNameHandling.Objects, IsReference = true, ItemIsReference = true, MemberSerialization = MemberSerialization.OptIn)]
    public abstract class DtoBase: IDto, INotifyPropertyChanged
    {
        public Guid DtoGuid { get; set; }

#if DEBUG
        ~DtoBase()
        {
            Debug.WriteLine(this, string.Format("{0} Finalized", GetType().FullName));
        }
#endif // DEBUG

        protected virtual bool SetField<T>(ref T field, T value, string propertyName)
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
