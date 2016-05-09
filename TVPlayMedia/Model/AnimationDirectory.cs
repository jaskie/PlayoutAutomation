using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Server.Interfaces;

namespace TAS.Client.Model
{
    public class AnimationDirectory : MediaDirectory, IAnimationDirectory
    {
        public override IMedia FindMediaByDto(Guid dtoGuid)
        {
            ServerMedia result = Query<ServerMedia>(parameters: new[] { dtoGuid });
            result.Directory = this;
            return result;
        }
    }
}
