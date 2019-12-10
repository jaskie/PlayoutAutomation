using System;

namespace TAS.Common.Interfaces.MediaDirectory
{
    public interface IServerDirectory: IWatcherDirectory
    {
        bool IsRecursive { get; }
        TMovieContainerFormat MovieContainerFormat { get; }
        event EventHandler<MediaIngestStatusEventArgs> IngestStatusUpdated;
    }

}
