using System;
using TAS.Common.Interfaces.Media;

namespace TAS.Common.Interfaces.MediaDirectory
{
    public interface IAnimationDirectory: IWatcherDirectory
    {
        IAnimatedMedia CloneMedia(IAnimatedMedia source, Guid newMediaGuid);
    }
}
