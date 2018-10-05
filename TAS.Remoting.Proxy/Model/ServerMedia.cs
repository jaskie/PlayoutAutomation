using Newtonsoft.Json;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;

namespace TAS.Remoting.Model
{
    public class ServerMedia : PersistentMedia, IServerMedia
    {
        #pragma warning disable CS0649

        [JsonProperty(nameof(IServerMedia.DoNotArchive))]
        private bool _doNotArchive;

        [JsonProperty(nameof(IServerMedia.IsArchived))]
        private bool _isArchived;

        #pragma warning restore

        public bool DoNotArchive
        {
            get => _doNotArchive;
            set => Set(value);
        }
        public bool IsArchived => _isArchived;
    }
}
