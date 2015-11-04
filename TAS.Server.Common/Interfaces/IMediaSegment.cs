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
        TimeSpan TCIn { get; set; }
        TimeSpan TCOut { get; set; }
        Guid MediaGuid { get; }
        void Save();
        void Delete();
    }
}
