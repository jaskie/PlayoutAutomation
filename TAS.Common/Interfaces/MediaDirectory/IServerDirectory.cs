using System.Collections.Generic;
using TAS.Common.Interfaces.Media;

namespace TAS.Common.Interfaces.MediaDirectory
{
    public interface IServerDirectory: IWatcherDirectory
    {
        bool IsRecursive { get; }
        TMovieContainerFormat MovieContainerFormat { get; }
        bool IsPrimary { get; }
    }

}
