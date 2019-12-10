using System;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.MediaDirectory;

namespace TAS.Remoting.Model.Media
{
    public class AnimationDirectory : WatcherDirectory, IAnimationDirectory
    {
        public void CloneMedia(IAnimatedMedia source, Guid newMediaGuid)
        {
            Invoke(parameters: new object[] { source });
        }
    }
}
