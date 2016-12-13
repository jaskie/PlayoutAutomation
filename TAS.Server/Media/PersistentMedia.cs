using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.ComponentModel;
using System.Runtime.Remoting.Messaging;
using System.Diagnostics;
using TAS.Common;
using TAS.Server.Interfaces;
using TAS.Server.Common;
using TAS.Server.Database;
using Newtonsoft.Json;

namespace TAS.Server
{
    public abstract class PersistentMedia: Media, IPersistentMedia
    {
        internal PersistentMedia(IMediaDirectory directory, Guid guid, UInt64 idPersistentMedia) : base(directory, guid) { IdPersistentMedia = idPersistentMedia; }
        public UInt64 IdPersistentMedia { get; set; }

        // media properties

        internal DateTime _killDate;
        [JsonProperty]
        public DateTime KillDate
        {
            get { return _killDate; }
            set { SetField(ref _killDate, value, nameof(KillDate)); }
        }

        // content properties
        [JsonProperty]
        public UInt64 IdProgramme { get; set; }
        internal string _idAux;
        [JsonProperty]
        public string IdAux
        {
            get { return _idAux; }
            set { SetField(ref _idAux, value, nameof(IdAux)); }
        } // auxiliary Id from external system

        internal TMediaEmphasis _mediaEmphasis;
        [JsonProperty]
        public TMediaEmphasis MediaEmphasis
        {
            get { return _mediaEmphasis; }
            set { SetField(ref _mediaEmphasis, value, nameof(MediaEmphasis)); }
        }

        protected bool _protected;
        [JsonProperty]
        public bool Protected
        {
            get { return _protected; }
            set { SetField(ref _protected, value, nameof(Protected)); }
        }

        private ObservableSynchronizedCollection<IMediaSegment> _mediaSegments;

        public ObservableSynchronizedCollection<IMediaSegment> MediaSegments
        {
            get
            {
                if (_mediaSegments == null)
                    _mediaSegments = this.DbMediaSegmentsRead<MediaSegment>();
                return _mediaSegments;
            }
        }

        public IMediaSegment CreateSegment()
        {
            return new MediaSegment(this.MediaGuid);
        }

        public override void CloneMediaProperties(IMediaProperties fromMedia)
        {
            base.CloneMediaProperties(fromMedia);
            if (fromMedia is IPersistentMedia)
            {
                IdAux = (fromMedia as IPersistentMedia).IdAux;
                IdProgramme = (fromMedia as IPersistentMedia).IdProgramme;
                MediaEmphasis = (fromMedia as IPersistentMedia).MediaEmphasis;
            }
        }

        public abstract bool Save();

        public bool IsModified { get; set; }

        protected override bool SetField<T>(ref T field, T value, string propertyName)
        {
            bool modified = base.SetField<T>(ref field, value, propertyName);
            if (modified && propertyName != nameof(IsVerified)) 
                IsModified = true; 
            return modified;
        }

        internal override void Verify()
        {
            base.Verify();
            if (IsModified)
                Save();
        }
    }
}
