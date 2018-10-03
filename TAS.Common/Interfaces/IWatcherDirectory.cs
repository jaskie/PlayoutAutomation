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

        event EventHandler<MediaEventArgs> MediaVerified;
    }


}
