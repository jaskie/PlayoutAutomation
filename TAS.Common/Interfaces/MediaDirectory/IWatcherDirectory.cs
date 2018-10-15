using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TAS.Common.Interfaces.Media;

namespace TAS.Common.Interfaces.MediaDirectory
{
    public interface IWatcherDirectory : IMediaDirectory, IDisposable
    {
        bool IsInitialized { get; }

        Task<IEnumerable<IMedia>> GetFiles();

        void SweepStaleMedia();

        Task Refresh();

        event EventHandler<MediaEventArgs> MediaVerified;
    }


}
