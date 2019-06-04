using System;
using System.ComponentModel;
using TAS.Common.Interfaces.Media;

namespace TAS.Common.Interfaces.MediaDirectory
{
    public interface IMediaDirectory: IMediaDirectoryProperties, INotifyPropertyChanged
    {
        long VolumeTotalSize { get; }
        long VolumeFreeSize { get; }
        char PathSeparator { get; }
        bool DirectoryExists();
        bool FileExists(string filename, string subfolder = null);
        bool HaveFileWatcher { get; }
        string GetUniqueFileName(string fileName);
        IMediaSearchProvider Search(TMediaCategory? category, string searchString);
        event EventHandler<MediaEventArgs> MediaAdded;
        event EventHandler<MediaEventArgs> MediaRemoved;
    }

    public interface IMediaDirectoryProperties
    {
        string Folder { get; set; }
    }

    public interface IMediaDirectoryServerSide: IMediaDirectory
    {
        void AddMedia(IMedia media);
        void RemoveMedia(IMedia media);
    }
}
