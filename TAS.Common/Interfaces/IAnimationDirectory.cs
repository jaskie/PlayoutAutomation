using System;

namespace TAS.Common.Interfaces
{
    public interface IAnimationDirectory: IWatcherDirectory
    {
        IAnimatedMedia CloneMedia(IAnimatedMedia source, Guid newMediaGuid);
    }
}
