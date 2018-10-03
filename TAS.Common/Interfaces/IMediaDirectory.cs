using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TAS.Common.Interfaces
{
    public interface IMediaDirectory
    {
        string DirectoryName { get; set; }
        string Folder { get; set; }
        long VolumeTotalSize { get; }
        long VolumeFreeSize { get; }
        char PathSeparator { get; }
        bool DirectoryExists();
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
