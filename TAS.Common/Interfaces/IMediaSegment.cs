using System;
using System.ComponentModel;

namespace TAS.Common.Interfaces
{
    public interface IMediaSegment: INotifyPropertyChanged, IPersistent
    {
        string SegmentName { get; set; }
        TimeSpan TcIn { get; set; }
        TimeSpan TcOut { get; set; }
        IMediaSegments Owner { get; }
    }
}
