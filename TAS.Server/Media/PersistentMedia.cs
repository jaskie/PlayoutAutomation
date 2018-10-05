using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;

namespace TAS.Server.Media
{
    public abstract class PersistentMedia: MediaBase, IPersistentMedia
    {

        private DateTime _killDate;
        private string _idAux;
        private TMediaEmphasis _mediaEmphasis;
        private bool _protected;
        private readonly Lazy<MediaSegments> _mediaSegments;

        protected PersistentMedia() 
        {
            _mediaSegments = new Lazy<MediaSegments>(() => EngineController.Database.DbMediaSegmentsRead<MediaSegments>(this));
        }
        public ulong IdPersistentMedia { get; set; }

        // media properties

        [JsonProperty]
        public DateTime KillDate
        {
            get => _killDate;
            set => SetField(ref _killDate, value);
        }

        // content properties
        [JsonProperty]
        public ulong IdProgramme { get; set; }

        [JsonProperty]
        public string IdAux
        {
            get => _idAux;
            set => SetField(ref _idAux, value);
        } // auxiliary Id from external system

        [JsonProperty]
        public TMediaEmphasis MediaEmphasis
        {
            get => _mediaEmphasis;
            set => SetField(ref _mediaEmphasis, value);
        }

        [JsonProperty]
        public bool Protected
        {
            get => _protected;
            set => SetField(ref _protected, value);
        }

        public IMediaSegments GetMediaSegments() => _mediaSegments.Value;

        public abstract IDictionary<string, int> FieldLengths { get; } 


        internal override void CloneMediaProperties(IMediaProperties fromMedia)
        {
            base.CloneMediaProperties(fromMedia);
            if (!(fromMedia is IPersistentMediaProperties properties))
                return;
            IdAux = properties.IdAux;
            IdProgramme = properties.IdProgramme;
            MediaEmphasis = properties.MediaEmphasis;
        }

        public abstract bool Save();

        public bool IsModified { get; set; }

        public override void Verify()
        {
            base.Verify();
            if (IsModified)
                Save();
        }

        protected override bool SetField<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            bool modified = base.SetField(ref field, value, propertyName);
            if (modified && propertyName != nameof(IsVerified))
                IsModified = true;
            return modified;
        }
    }
}
