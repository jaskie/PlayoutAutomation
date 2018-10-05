using System;
using System.ComponentModel;
using TAS.Common.Interfaces.Media;

namespace TAS.Common.Interfaces.MediaDirectory
{
    public interface IMediaDirectory: INotifyPropertyChanged
    {
        string DirectoryName { get; set; }
        string Folder { get; set; }
        long VolumeTotalSize { get; }
        long VolumeFreeSize { get; }
        char PathSeparator { get; }
        bool DirectoryExists();
        bool FileExists(string filename, string subfolder = null);

        string GetUniqueFileName(string fileName);

        event EventHandler<MediaEventArgs> MediaAdded;
        event EventHandler<MediaEventArgs> MediaRemoved;
    }

    public interface IMediaDirectoryServerSide
    {
        void AddMedia(IMedia media);
        void RemoveMedia(IMedia media);
        IMedia CreateMedia(IMediaProperties mediaProperties);
    }
}
