using System;
using System.Collections.Generic;
using TAS.Common.Interfaces.Media;

namespace TAS.Common.Interfaces.MediaDirectory
{
    public interface IWatcherDirectory : IMediaDirectory
    {
        bool IsInitialized { get; }

        void SweepStaleMedia();

        void Refresh();

        IReadOnlyCollection<IMedia> GetAllFiles();

        event EventHandler<MediaEventArgs> MediaVerified;
        
    }
}
