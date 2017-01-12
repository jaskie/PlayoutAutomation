using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using TAS.Common;
using TAS.Server.Common;

namespace TAS.Server.Interfaces
{
    public interface IMediaDirectory : IMediaDirectoryProperties, INotifyPropertyChanged, IDisposable
    {
        bool FileExists(string filename, string subfolder = null);
        bool DirectoryExists();
        bool IsInitialized { get; }
        ICollection<IMedia> GetFiles();
        void Refresh();
        void SweepStaleMedia();
        long VolumeTotalSize { get; }
        long VolumeFreeSize { get; }
        char PathSeparator { get; }
        IMedia CreateMedia(IMediaProperties mediaProperties);

        event EventHandler<MediaEventArgs> MediaAdded;
        event EventHandler<MediaEventArgs> MediaRemoved;
        event EventHandler<MediaEventArgs> MediaVerified;
        event EventHandler<MediaEventArgs> MediaDeleted;
    }

    public interface IMediaDirectoryProperties
    {
        string DirectoryName { get; set; }
        string Folder { get; set; }
    }
}
