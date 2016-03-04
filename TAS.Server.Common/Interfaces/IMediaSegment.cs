using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using TAS.Remoting;

namespace TAS.Server.Interfaces
{
    public interface IMediaSegment: IDto, INotifyPropertyChanged
    {
        UInt64 IdMediaSegment { get; }
        string SegmentName { get; set; }
        TimeSpan TcIn { get; set; }
        TimeSpan TcOut { get; set; }
        Guid MediaGuid { get; }
        void Save();
        void Delete();
    }
}
