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
        internal PersistentMedia(IMediaDirectory directory, Guid guid, UInt64 idPersistentMedia) : base(directory, guid)
        {
            IdPersistentMedia = idPersistentMedia;
            _mediaSegments = new Lazy<MediaSegments>(() => this.DbMediaSegmentsRead<MediaSegments>());
        }
        public UInt64 IdPersistentMedia { get; set; }

        // media properties

        internal DateTime _killDate;
        [JsonProperty]
        public DateTime KillDate
        {
            get { return _killDate; }
            set { SetField(ref _killDate, value); }
        }

        // content properties
        [JsonProperty]
        public UInt64 IdProgramme { get; set; }
        internal string _idAux;
        [JsonProperty]
        public string IdAux
        {
            get { return _idAux; }
            set { SetField(ref _idAux, value); }
        } // auxiliary Id from external system

        internal TMediaEmphasis _mediaEmphasis;
        [JsonProperty]
        public TMediaEmphasis MediaEmphasis
        {
            get { return _mediaEmphasis; }
            set { SetField(ref _mediaEmphasis, value); }
        }

        protected bool _protected;
        [JsonProperty]
        public bool Protected
        {
            get { return _protected; }
            set { SetField(ref _protected, value); }
        }
        private readonly Lazy<MediaSegments> _mediaSegments;
        public IMediaSegments MediaSegments
        {
            get
            {
                return _mediaSegments.Value;
            }
        }

        public override void CloneMediaProperties(IMediaProperties fromMedia)
        {
            base.CloneMediaProperties(fromMedia);
            if (fromMedia is IPersistentMediaProperties)
            {
                IdAux = (fromMedia as IPersistentMediaProperties).IdAux;
                IdProgramme = (fromMedia as IPersistentMediaProperties).IdProgramme;
                MediaEmphasis = (fromMedia as IPersistentMediaProperties).MediaEmphasis;
            }
        }

        public abstract bool Save();

        public bool IsModified { get; set; }

        protected override bool SetField<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            bool modified = base.SetField(ref field, value, propertyName);
            if (modified && propertyName != nameof(IsVerified)) 
                IsModified = true; 
            return modified;
        }

        public override void Verify()
        {
            base.Verify();
            if (IsModified)
                Save();
        }
    }
}
