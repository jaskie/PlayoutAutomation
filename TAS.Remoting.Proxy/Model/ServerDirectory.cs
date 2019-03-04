using System.Collections.Generic;
using System.Collections.ObjectModel;
using Newtonsoft.Json;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.MediaDirectory;

namespace TAS.Remoting.Model
{
    public class ServerDirectory : WatcherDirectory, IServerDirectory
    {
        #pragma warning disable CS0649

        [JsonProperty(nameof(IServerDirectory.IsRecursive))]
        private bool _isRecursive;

        #pragma warning restore

        public override IReadOnlyCollection<IMedia> GetFiles()
        {
            return Query<ReadOnlyCollection<IMedia>>();
        }

        public bool IsRecursive => _isRecursive;
    }
}
