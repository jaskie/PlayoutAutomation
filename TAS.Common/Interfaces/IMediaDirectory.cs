using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace TAS.Common.Interfaces
{
    public interface IMediaDirectory : IMediaDirectoryProperties, INotifyPropertyChanged, IDisposable
    {
        bool IsInitialized { get; }
        long VolumeTotalSize { get; }
        long VolumeFreeSize { get; }
        char PathSeparator { get; }

        bool DirectoryExists();
        IEnumerable<IMedia> GetFiles();
        void Refresh();
        void SweepStaleMedia();
        IMedia CreateMedia(IMediaProperties mediaProperties);
        bool FileExists(string filename, string subfolder = null);
        string GetUniqueFileName(string fileName);

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
