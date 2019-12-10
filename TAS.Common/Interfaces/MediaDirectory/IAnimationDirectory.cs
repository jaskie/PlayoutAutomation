using System;
using TAS.Common.Interfaces.Media;

namespace TAS.Common.Interfaces.MediaDirectory
{
    public interface IAnimationDirectory: IWatcherDirectory
    {
        void CloneMedia(IAnimatedMedia source, Guid newMediaGuid);
    }
}
