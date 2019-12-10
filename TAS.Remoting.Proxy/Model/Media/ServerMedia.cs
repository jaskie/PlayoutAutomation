using Newtonsoft.Json;
using TAS.Common.Interfaces.Media;

namespace TAS.Remoting.Model.Media
{
    public class ServerMedia : PersistentMedia, IServerMedia
    {
        #pragma warning disable CS0649

        [JsonProperty(nameof(IServerMedia.DoNotArchive))]
        private bool _doNotArchive;

        #pragma warning restore

        public bool DoNotArchive
        {
            get => _doNotArchive;
            set => Set(value);
        }

    }
}
