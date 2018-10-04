using System;
using System.Collections.Generic;

namespace TAS.Common.Interfaces
{
    public interface IWatcherDirectory : IMediaDirectory, IDisposable
    {
        bool IsInitialized { get; }

        IEnumerable<IMedia> GetFiles();

        void SweepStaleMedia();

        void Refresh();

        event EventHandler<MediaEventArgs> MediaVerified;
    }


}
