using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace TAS.Server.Interfaces
{
    public interface IMediaSegment: INotifyPropertyChanged
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
