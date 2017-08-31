using System;
using Newtonsoft.Json;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Remoting.Model
{
    public class PersistentMedia : MediaBase, IPersistentMedia
    {
        #pragma warning disable CS0649 

        [JsonProperty(nameof(IPersistentMedia.MediaEmphasis))]
        private TMediaEmphasis _mediaEmphasis;

        [JsonProperty(nameof(IPersistentMedia.IdAux))]
        private string _idAux;

        [JsonProperty(nameof(IPersistentMedia.KillDate))]
        private DateTime _killDate;

        [JsonProperty(nameof(IPersistentMedia.IdProgramme))]
        private ulong _idProgramme;

        [JsonProperty(nameof(IPersistentMedia.IdPersistentMedia))]
        private ulong _idPersistentMedia;

        [JsonProperty(nameof(IPersistentMedia.IsModified))]
        private bool _isModified;

        [JsonProperty(nameof(IPersistentMedia.Protected))]
        private bool _protected;

        #pragma warning restore

        public TMediaEmphasis MediaEmphasis { get { return _mediaEmphasis; } set { Set(value); } }

        public string IdAux { get { return _idAux; } set { Set(value); } }

        public DateTime KillDate { get { return _killDate; } set { Set(value); } }

        public ulong IdProgramme { get { return _idProgramme; } set { Set(value); } }

        public ulong IdPersistentMedia { get; set; }

        public bool IsModified { get { return _isModified; } set { Set(value); } }

        public bool Protected { get { return _protected; } set { Set(value); } }

        public IMediaSegments GetMediaSegments() { return Query<MediaSegments>(); } 

        public bool Save()
        {
            return Query<bool>();
        }
    }
}
