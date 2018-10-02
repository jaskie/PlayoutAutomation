using System;
using System.Collections.Generic;
using TAS.Common.Interfaces;

namespace TAS.Remoting.Model
{
    public class AnimationDirectory : WatcherDirectory, IAnimationDirectory
    {
        public IAnimatedMedia CloneMedia(IAnimatedMedia source, Guid newMediaGuid)
        {
            return Query<IAnimatedMedia>(parameters: new object[] { source });
        }

        public override IMedia CreateMedia(IMediaProperties mediaProperties)
        {
            return Query<IAnimatedMedia>(parameters: new object[] { mediaProperties });
        }

        public override IEnumerable<IMedia> GetFiles()
        {
            return Query<List<AnimatedMedia>>();
        }

    }
}
