using System;
using System.Collections.Generic;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.MediaDirectory;

namespace TAS.Remoting.Model
{
    public class AnimationDirectory : MediaDirectoryBase, IAnimationDirectory
    {
        public IAnimatedMedia CloneMedia(IAnimatedMedia source, Guid newMediaGuid)
        {
            return Query<IAnimatedMedia>(parameters: new object[] { source });
        }

        public override IEnumerable<IMedia> GetFiles()
        {
            return Query<List<AnimatedMedia>>();
        }

    }
}
