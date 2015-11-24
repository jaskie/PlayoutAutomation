using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using TAS.Common;
using TAS.Server.Common;

namespace TAS.Server.Interfaces
{
    public interface IMediaDirectory : IMediaDirectoryConfig, IDto, INotifyPropertyChanged, IDisposable
    {
        bool FileExists(string filename, string subfolder = null);
        TDirectoryAccessType AccessType { get; }
        System.Net.NetworkCredential NetworkCredential { get; }
        bool IsInitialized { get; }
        List<IMedia> Files { get; }
        void Initialize();
        void Refresh();
        void SweepStaleMedia();
        IMedia FindMediaDto(Guid guidDto);
        UInt64 VolumeTotalSize { get; }
        UInt64 VolumeFreeSize { get; }

        event EventHandler<GuidEventArgs> MediaAdded;
        event EventHandler<GuidEventArgs> MediaRemoved;
        event EventHandler<GuidEventArgs> MediaVerified;
    }
}
