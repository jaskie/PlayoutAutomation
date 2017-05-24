using System;

namespace TAS.Server.Common.Interfaces
{
    public interface IAnimationDirectory: IMediaDirectory
    {
        IAnimatedMedia CloneMedia(IAnimatedMedia source, Guid newMediaGuid);
    }
}
