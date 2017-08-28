using System;

namespace TAS.Common.Interfaces
{
    public interface IAnimationDirectory: IMediaDirectory
    {
        IAnimatedMedia CloneMedia(IAnimatedMedia source, Guid newMediaGuid);
    }
}
