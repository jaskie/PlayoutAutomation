using System;
using System.Collections.Generic;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.MediaDirectory;

namespace TAS.Remoting.Model
{
    public class AnimationDirectory : WatcherDirectory, IAnimationDirectory
    {
        public void CloneMedia(IAnimatedMedia source, Guid newMediaGuid)
        {
            Invoke(parameters: new object[] { source });
        }

        public override IReadOnlyCollection<IMedia> GetFiles()
        {
            return Query<List<AnimatedMedia>>();
        }

    }
}
