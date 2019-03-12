using System;
using Newtonsoft.Json;
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

        private readonly Lazy<bool> _isArchivedLazy;

        #pragma warning restore

        public ServerMedia()
        {
            _isArchivedLazy = new Lazy<bool>(() =>
            {
                _isArchived = Get<bool>(nameof(IsArchived));
                return _isArchived;
            });
        }

        public bool DoNotArchive
        {
            get => _doNotArchive;
            set => Set(value);
        }

        public bool IsArchived => _isArchivedLazy.IsValueCreated ? _isArchived : _isArchivedLazy.Value;
    }
}
