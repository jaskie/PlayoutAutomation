using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using TAS.Common;
using TAS.Remoting;
using TAS.Server.Common;

namespace TAS.Server.Interfaces
{
    public interface IMediaDirectory : IMediaDirectoryConfig, IDto, INotifyPropertyChanged, IDisposable
    {
        bool FileExists(string filename, string subfolder = null);
        bool DirectoryExists();
        bool IsInitialized { get; }
        ICollection<IMedia> GetFiles();
        void Initialize();
        void Refresh();
        void SweepStaleMedia();
        IMedia FindMediaByDto(Guid guidDto);
        long VolumeTotalSize { get; }
        long VolumeFreeSize { get; }
        char PathSeparator { get; }

        event EventHandler<MediaDtoEventArgs> MediaAdded;
        event EventHandler<MediaDtoEventArgs> MediaRemoved;
        event EventHandler<MediaDtoEventArgs> MediaVerified;
        event EventHandler<MediaDtoEventArgs> MediaDeleted;
    }
}
