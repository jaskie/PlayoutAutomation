using System;
using Newtonsoft.Json;
using TAS.Common;
using TAS.Database;
using TAS.Common.Interfaces;

namespace TAS.Server.Media
{
    public abstract class PersistentMedia: MediaBase, IPersistentMedia
    {

        private DateTime _killDate;
        private string _idAux;
        private TMediaEmphasis _mediaEmphasis;
        private bool _protected;
        private readonly Lazy<MediaSegments> _mediaSegments;

        internal PersistentMedia(IMediaDirectory directory, Guid guid, UInt64 idPersistentMedia) : base(directory, guid)
        {
            IdPersistentMedia = idPersistentMedia;
            _mediaSegments = new Lazy<MediaSegments>(this.DbMediaSegmentsRead<MediaSegments>);
        }
        public UInt64 IdPersistentMedia { get; set; }

        // media properties

        [JsonProperty]
        public DateTime KillDate
        {
            get { return _killDate; }
            set { SetField(ref _killDate, value); }
        }

        // content properties
        [JsonProperty]
        public ulong IdProgramme { get; set; }
        [JsonProperty]
        public string IdAux
        {
            get { return _idAux; }
            set { SetField(ref _idAux, value); }
        } // auxiliary Id from external system

        [JsonProperty]
        public TMediaEmphasis MediaEmphasis
        {
            get { return _mediaEmphasis; }
            set { SetField(ref _mediaEmphasis, value); }
        }

        [JsonProperty]
        public bool Protected
        {
            get { return _protected; }
            set { SetField(ref _protected, value); }
        }
        public IMediaSegments MediaSegments => _mediaSegments.Value;

        public override void CloneMediaProperties(IMediaProperties fromMedia)
        {
            base.CloneMediaProperties(fromMedia);
            var properties = fromMedia as IPersistentMediaProperties;
            if (properties != null)
            {
                IdAux = properties.IdAux;
                IdProgramme = properties.IdProgramme;
                MediaEmphasis = properties.MediaEmphasis;
            }
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
