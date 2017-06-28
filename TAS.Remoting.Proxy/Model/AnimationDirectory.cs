using System;
using TAS.Server.Common.Interfaces;

namespace TAS.Remoting.Model
{
    public class AnimationDirectory : MediaDirectory, IAnimationDirectory
    {
        public IAnimatedMedia CloneMedia(IAnimatedMedia source, Guid newMediaGuid)
        {
            return Query<IAnimatedMedia>(parameters: new object[] { source });
        }

        public override IMedia CreateMedia(IMediaProperties mediaProperties)
        {
            return Query<IAnimatedMedia>(parameters: new object[] { mediaProperties });
        }
    }
}
