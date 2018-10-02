using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace TAS.Common.Interfaces
{
    public interface IWatcherDirectory : IMediaDirectory, INotifyPropertyChanged, IDisposable
    {
        bool IsInitialized { get; }

        IEnumerable<IMedia> GetFiles();

        void SweepStaleMedia();

        bool FileExists(string filename, string subfolder = null);

        string GetUniqueFileName(string fileName);
        void Refresh();

        event EventHandler<MediaEventArgs> MediaAdded;
        event EventHandler<MediaEventArgs> MediaRemoved;
        event EventHandler<MediaEventArgs> MediaVerified;
        event EventHandler<MediaEventArgs> MediaDeleted;
    }

    public interface IMediaDirectory
    {
        string DirectoryName { get; set; }
        string Folder { get; set; }
        long VolumeTotalSize { get; }
        long VolumeFreeSize { get; }
        char PathSeparator { get; }
        bool DirectoryExists();
    }

    public interface IMediaDirectoryServerSide
    {
        void AddMedia(IMedia media);
        void RemoveMedia(IMedia media);
        IMedia CreateMedia(IMediaProperties mediaProperties);
    }
}
