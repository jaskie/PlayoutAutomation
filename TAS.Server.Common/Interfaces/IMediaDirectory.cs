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
        IEnumerable<IMedia> GetFiles();
        void Initialize();
        void Refresh();
        void SweepStaleMedia();
        IMedia FindMediaByDto(Guid guidDto);
        UInt64 VolumeTotalSize { get; }
        UInt64 VolumeFreeSize { get; }

        event EventHandler<MediaDtoEventArgs> MediaAdded;
        event EventHandler<MediaDtoEventArgs> MediaRemoved;
        event EventHandler<MediaDtoEventArgs> MediaVerified;
    }
}
