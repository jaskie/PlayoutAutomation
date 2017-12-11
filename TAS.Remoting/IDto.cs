using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace TAS.Remoting
{
    public interface IDto: INotifyPropertyChanged, IDisposable
    {
        Guid DtoGuid { get; }
        event EventHandler Disposed;
    }
}
