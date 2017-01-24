using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace TAS.Server.Interfaces
{
    public interface IMediaSegment: INotifyPropertyChanged
    {
        string SegmentName { get; set; }
        TimeSpan TcIn { get; set; }
        TimeSpan TcOut { get; set; }
        void Save();
        void Delete();
        IMediaSegments Owner { get; }
    }
}
