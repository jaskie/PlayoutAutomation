using System;
using System.ComponentModel;

namespace TAS.Remoting
{
    public interface IDto: INotifyPropertyChanged, IDisposable
    {
        Guid DtoGuid { get; }
        void Release();
        event EventHandler Disposed;
    }
}
