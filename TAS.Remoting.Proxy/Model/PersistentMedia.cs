using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;

namespace TAS.Remoting.Model
{
    public abstract class PersistentMedia : MediaBase, IPersistentMedia
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

        [JsonProperty(nameof(FieldLengths))]
        private Dictionary<string, int> _fieldsLengths;

#pragma warning restore

        public TMediaEmphasis MediaEmphasis { get => _mediaEmphasis; set => Set(value); }

        public string IdAux { get => _idAux; set => Set(value); }

        public DateTime KillDate { get => _killDate; set => Set(value); }

        public ulong IdProgramme { get => _idProgramme; set => Set(value); }

        public ulong IdPersistentMedia { get => _idPersistentMedia; set => Set(value); }

        public bool IsModified { get => _isModified; set => Set(value); }

        public bool Protected { get => _protected; set => Set(value); }

        public IDictionary<string, int> FieldLengths { get => _fieldsLengths; set => Set(value); }

        public IMediaSegments GetMediaSegments() { return Query<MediaSegments>(); }

        public bool Save()
        {
            return Query<bool>();
        }
    }
}
