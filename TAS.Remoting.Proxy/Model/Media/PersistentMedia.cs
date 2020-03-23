using jNet.RPC;
using System;
using System.Collections.Generic;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;

namespace TAS.Remoting.Model.Media
{
    public abstract class PersistentMedia : MediaBase, IPersistentMedia
    {
#pragma warning disable CS0649

        [DtoField(nameof(IPersistentMedia.MediaEmphasis))]
        private TMediaEmphasis _mediaEmphasis;

        [DtoField(nameof(IPersistentMedia.IdAux))]
        private string _idAux;

        [DtoField(nameof(IPersistentMedia.KillDate))]
        private DateTime? _killDate;

        [DtoField(nameof(IPersistentMedia.IdProgramme))]
        private ulong _idProgramme;

        [DtoField(nameof(IPersistentMedia.IsProtected))]
        private bool _protected;

        [DtoField(nameof(FieldLengths))]
        private Dictionary<string, int> _fieldsLengths;

#pragma warning restore

        public TMediaEmphasis MediaEmphasis { get => _mediaEmphasis; set => Set(value); }

        public string IdAux { get => _idAux; set => Set(value); }

        public DateTime? KillDate { get => _killDate; set => Set(value); }

        public ulong IdProgramme { get => _idProgramme; set => Set(value); }

        public bool IsProtected { get => _protected; set => Set(value); }

        public IDictionary<string, int> FieldLengths { get => _fieldsLengths; set => Set(value); }

        public IMediaSegments GetMediaSegments() { return Query<MediaSegments>(); }

        public void Save() => Invoke();
    }
}
