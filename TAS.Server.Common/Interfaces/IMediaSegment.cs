using System;
using System.ComponentModel;

namespace TAS.Server.Common.Interfaces
{
    public interface IMediaSegment: INotifyPropertyChanged
    {
        string SegmentName { get; set; }
        TimeSpan TcIn { get; set; }
        TimeSpan TcOut { get; set; }
        void Save();
        void Delete();
        
    }
    public interface IMediaSegmentPersistent : IPersistent
    {
        IMediaSegments Owner { get; }
    }

}
