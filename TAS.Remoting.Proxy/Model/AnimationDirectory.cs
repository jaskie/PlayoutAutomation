using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.MediaDirectory;

namespace TAS.Remoting.Model
{
    public class AnimationDirectory : WatcherDirectory, IAnimationDirectory
    {
        public IAnimatedMedia CloneMedia(IAnimatedMedia source, Guid newMediaGuid)
        {
            return Query<IAnimatedMedia>(parameters: new object[] { source });
        }

        public override async Task<IEnumerable<IMedia>> GetFiles()
        {
            return await Task.Run(() => Query<List<AnimatedMedia>>());
        }

    }
}
