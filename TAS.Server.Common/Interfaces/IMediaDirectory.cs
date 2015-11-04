using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using TAS.Common;
using TAS.Server.Common;

namespace TAS.Server.Interfaces
{
    public interface IMediaDirectory: IMediaDirectoryConfig, INotifyPropertyChanged, IDisposable
    {
        IMedia FindMedia(IMedia media);
        IMedia FindMedia(Guid mediaGuid);
        bool FileExists(string filename, string subfolder = null);
        List<IMedia> FindMedia(Func<IMedia, bool> condition);
        TDirectoryAccessType AccessType { get; }
        System.Net.NetworkCredential NetworkCredential { get; }
        void MediaAdd(IMedia media);
        void MediaRemove(IMedia media);
        bool DeleteMedia(IMedia media);
        void OnMediaVerified(IMedia media);
        bool IsInitialized { get; }
        List<IMedia> Files { get; }
        void Initialize();
        void Refresh();
        UInt64 VolumeTotalSize { get; }
        UInt64 VolumeFreeSize { get; }

        event EventHandler<MediaEventArgs> MediaAdded;
        event EventHandler<MediaEventArgs> MediaRemoved;
        event EventHandler<MediaEventArgs> MediaVerified;
    }
}
