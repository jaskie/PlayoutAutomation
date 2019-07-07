using System;
using Newtonsoft.Json;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.MediaDirectory;
using TAS.Remoting.Model.Media;

namespace TAS.Remoting.Model
{
    public class AnimationDirectory : WatcherDirectory, IAnimationDirectory
    {
#pragma warning disable CS0649

        [JsonProperty(nameof(IAnimationDirectory.IsPrimary))]
        private readonly bool _isPrimary;
#pragma warning restore

        public void CloneMedia(IAnimatedMedia source, Guid newMediaGuid)
        {
            Invoke(parameters: new object[] { source });
        }

        public bool IsPrimary => _isPrimary;

    }
}
