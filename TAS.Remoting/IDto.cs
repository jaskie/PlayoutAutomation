using System;
using System.ComponentModel;

namespace TAS.Remoting
{
    public interface IDto: INotifyPropertyChanged, IDisposable
    {
        Guid DtoGuid { get; }
        event EventHandler Disposed;
    }
}
