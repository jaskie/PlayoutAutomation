using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using TAS.Server.Interfaces;

namespace TAS.Client.Model
{
    public class AnimationDirectory : MediaDirectory, IAnimationDirectory
    {

        public IAnimatedMedia CloneMedia(IAnimatedMedia source, Guid newMediaGuid)
        {
            return Query<IAnimatedMedia>(parameters: new[] { source });
        }

        public override IMedia CreateMedia(IMediaProperties mediaProperties)
        {
            return Query<IAnimatedMedia>(parameters: new[] { mediaProperties });
        }
    }
}
